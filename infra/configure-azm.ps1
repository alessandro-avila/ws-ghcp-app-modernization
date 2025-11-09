######################################################
##############   CONFIGURATIONS   ###################
######################################################

$SkillableEnvironment = $false
$environmentName = "crgmig21" # Set your environment name here for non-Skillable environments

# Environment name and prefix for all azure resources
if ($SkillableEnvironment) {
    $environmentName = "lab@lab.LabInstance.ID"
    $resourceGroup = "on-prem"
}
else {
    # Configuration
$SkillableEnvironment = $true
$environmentName = "teamXY"

# Storage account configuration for logging in Skillable environments
$STORAGE_ACCOUNT_NAME = "sttestuploads"
$CONTAINER_NAME = "script-logs"  
$LOG_BLOB_NAME = "${environmentName}-configure-azm-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$STORAGE_SAS_TOKEN = "?sv=2022-11-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2024-12-31T22:39:06Z&st=2024-04-10T14:39:06Z&spr=https,http&sig=MwUc4sXO5sJJFAy7GF8LBp49Nqb4CtJ6zHq%2BD2Lg5P0%3D"
}

# Blob used to send log messages
$STORAGE_SAS_TOKEN = "?sv=2024-11-04&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=2026-01-30T22:09:19Z&st=2025-11-05T13:54:19Z&spr=https&sig=mBoL3bVHPGSniTeFzXZ5QdItTxaFYOrhXIOzzM2jvF0%3D"
$STORAGE_ACCOUNT_NAME = "azmdeploymentlogs"
$CONTAINER_NAME = "logs"
$LOG_BLOB_NAME = "$environmentName.log"

# API version constant
$apiVersionOffAzure = "2024-12-01-preview"

######################################################
##############   INFRASTRUCTURE FUNCTIONS   #########
######################################################

function Import-AzureModules {
    Write-LogToBlob "Importing Azure PowerShell modules"
    
    # Ensure we're using Az modules and remove any AzureRM conflicts
    Import-Module Az.Accounts, Az.Resources -Force
    Get-Module -Name AzureRM* | Remove-Module -Force
    
    Write-LogToBlob "Azure PowerShell modules imported successfully"
}

function Get-AuthenticationHeaders {
    Write-LogToBlob "Getting access token for REST API calls"
    
    try {
        $accessTokenObject = Get-AzAccessToken -ResourceUrl "https://management.azure.com/"
        
        # Handle both SecureString and plain string token formats
        if ($accessTokenObject.Token -is [System.Security.SecureString]) {
            $token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($accessTokenObject.Token))
        }
        else {
            $token = $accessTokenObject.Token
        }

        $headers = @{
            "authorization" = "Bearer $token"
            "content-type"  = "application/json"
        }
        
        Write-LogToBlob "Authentication headers obtained successfully"
        
        return $headers
    }
    catch {
        Write-LogToBlob "Failed to get authentication headers: $($_.Exception.Message)" "ERROR"
        throw
    }
}
function New-AzureEnvironment {
    param(
        [string]$EnvironmentName
    )
    
    Write-LogToBlob "Creating Azure environment: $EnvironmentName"
    
    try {
        $resourceGroupName = "${EnvironmentName}-rg"
        $location = "swedencentral"
        $templateFile = '.\templates\lab197959-template2 (v6).json'
        
        Write-LogToBlob "Creating resource group: $resourceGroupName"
        New-AzResourceGroup -Name $resourceGroupName -Location $location -Force
        
        Write-LogToBlob "Deploying ARM template..."
        New-AzResourceGroupDeployment `
            -Name $EnvironmentName `
            -ResourceGroupName $resourceGroupName `
            -TemplateFile $templateFile `
            -prefix $EnvironmentName `
            -Verbose
        
        Write-LogToBlob "Azure environment created successfully"
    }
    catch {
        Write-LogToBlob "Failed to create Azure environment: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   LOGGING FUNCTIONS   ################
######################################################

function Write-LogToBlob {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    
    # Write to console
    Write-Host $logEntry
    
    if ($SkillableEnvironment -eq $false) {
        return
    }

    # Write to blob using Az.Storage commands
    try {
        # Create storage context using SAS token
        $ctx = New-AzStorageContext -StorageAccountName $STORAGE_ACCOUNT_NAME -SasToken $STORAGE_SAS_TOKEN
        
        # Get existing blob content to append
        $existingContent = ""
        try {
            Get-AzStorageBlobContent -Blob $LOG_BLOB_NAME -Container $CONTAINER_NAME -Context $ctx -Force -Destination "$env:TEMP\templog.txt" -ErrorAction Stop | Out-Null
            $existingContent = Get-Content "$env:TEMP\templog.txt" -Raw -ErrorAction SilentlyContinue
            Remove-Item "$env:TEMP\templog.txt" -Force -ErrorAction SilentlyContinue
        }
        catch {
            # Blob doesn't exist yet, that's fine
            Write-Host "Creating new log blob..." -ForegroundColor Yellow
        }
        
        # Append new log entry
        $newContent = $existingContent + $logEntry + "`n"
        
        # Write back to blob
        $tempFile = "$env:TEMP\$([System.Guid]::NewGuid()).txt"
        Set-Content -Path $tempFile -Value $newContent -NoNewline
        Set-AzStorageBlobContent -File $tempFile -Blob $LOG_BLOB_NAME -Container $CONTAINER_NAME -Context $ctx -Force | Out-Null
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        
    }
    catch {
        Write-Host "Failed to write log to blob: $($_.Exception.Message)" -ForegroundColor Red
        # Fallback to local file if blob fails
        $localLogFile = ".\script-execution.log"
        Add-Content -Path $localLogFile -Value $logEntry
    }
}

function Initialize-LogBlob {
    if (-not $SkillableEnvironment) {
        Write-LogToBlob "Skillable environment disabled, skipping blob logging initialization"
        return
    }

    try {
        $ctx = New-AzStorageContext -StorageAccountName $STORAGE_ACCOUNT_NAME -SasToken $STORAGE_SAS_TOKEN
        
        $initialLog = "=== Script execution started at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===`nEnvironment: $environmentName`n"
        
        $tempFile = "$env:TEMP\$([System.Guid]::NewGuid()).txt"
        Set-Content -Path $tempFile -Value $initialLog -NoNewline
        
        Set-AzStorageBlobContent -File $tempFile -Blob $LOG_BLOB_NAME -Container $CONTAINER_NAME -Context $ctx -Force | Out-Null
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        
        Write-Host "Initialized log blob: $LOG_BLOB_NAME" -ForegroundColor Green
        
    }
    catch {
        Write-Host "Failed to initialize log blob: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Check if storage account '$STORAGE_ACCOUNT_NAME' and container '$CONTAINER_NAME' exist" -ForegroundColor Red
        Write-Host "Also verify SAS token permissions and expiration" -ForegroundColor Red
        
        # Fallback to local file
        $localLogFile = ".\script-execution.log"
        $initialLog = "=== Script execution started at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===`nEnvironment: $environmentName`n"
        Set-Content -Path $localLogFile -Value $initialLog -NoNewline
        Write-Host "Created local log file as fallback: $localLogFile" -ForegroundColor Yellow
    }
}

######################################################
##############   MIGRATE TOOL FUNCTIONS   ###########
######################################################

function Register-MigrateTools {
    param(
        [string]$EnvironmentName,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Registering Azure Migrate tools"
    
    try {
        $subscriptionId = (Get-AzContext).Subscription.Id
        $resourceGroupName = if ($SkillableEnvironment) { "on-prem" } else { "${EnvironmentName}-rg" }
        $migrateProjectName = "${EnvironmentName}-azm"
        
        $registerToolApi = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Migrate/MigrateProjects/$migrateProjectName/registerTool?api-version=2020-06-01-preview"
        
        Write-LogToBlob "Registering Server Discovery tool"
        Write-LogToBlob "URI: $registerToolApi"
        Invoke-RestMethod -Uri $registerToolApi `
            -Method POST `
            -Headers $Headers `
            -ContentType 'application/json' `
            -Body '{"tool": "ServerDiscovery"}' | Out-Null
        Write-LogToBlob "Server Discovery tool registered successfully"

        Write-LogToBlob "Registering Server Assessment tool"
        Invoke-RestMethod -Uri $registerToolApi `
            -Method POST `
            -Headers $Headers `
            -ContentType 'application/json' `
            -Body '{"tool": "ServerAssessment"}' | Out-Null
        Write-LogToBlob "Server Assessment tool registered successfully"
    }
    catch {
        Write-LogToBlob "Failed to register Migrate tools: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   ARTIFACT FUNCTIONS   ###############
######################################################

function Get-DiscoveryArtifacts {
    Write-LogToBlob "Downloading discovery artifacts"
    
    try {
        $remoteZipFilePath = "https://github.com/crgarcia12/migrate-modernize-lab/raw/refs/heads/main/lab-material/Azure-Migrate-Discovery.zip"
        $localZipFilePath = Join-Path (Get-Location) "importArtifacts.zip"
        
        Write-LogToBlob "Downloading artifacts from: $remoteZipFilePath"
        Invoke-WebRequest $remoteZipFilePath -OutFile $localZipFilePath
        Write-LogToBlob "Downloaded artifacts to: $localZipFilePath"
        
        return $localZipFilePath
    }
    catch {
        Write-LogToBlob "Failed to download discovery artifacts: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Start-ArtifactImport {
    param(
        [string]$LocalZipFilePath,
        [string]$EnvironmentName,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Starting artifact import process"
    
    try {
        $subscriptionId = (Get-AzContext).Subscription.Id
        $resourceGroupName = if ($SkillableEnvironment) { "on-prem" } else { "${EnvironmentName}-rg" }
        $masterSiteName = "${EnvironmentName}mastersite"
        $apiVersionOffAzure = "2020-01-01"
        
        # Upload the ZIP file to OffAzure and start import
        $importUriUrl = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.OffAzure/masterSites/$masterSiteName/Import?api-version=$apiVersionOffAzure"
        Write-LogToBlob "Import URI: $importUriUrl"
        
        $importResponse = Invoke-RestMethod -Uri $importUriUrl -Method POST -Headers $Headers
        $blobUri = $importResponse.uri
        $jobArmId = $importResponse.jobArmId

        Write-LogToBlob "Blob URI: $blobUri"
        Write-LogToBlob "Job ARM ID: $jobArmId"

        Write-LogToBlob "Uploading ZIP to blob..."
        $fileBytes = [System.IO.File]::ReadAllBytes($LocalZipFilePath)
        $uploadBlobHeaders = @{
            "x-ms-blob-type" = "BlockBlob"
            "x-ms-version"   = "2020-04-08"
        }
        Invoke-RestMethod -Uri $blobUri -Method PUT -Headers $uploadBlobHeaders -Body $fileBytes -ContentType "application/octet-stream"
        Write-LogToBlob "Successfully uploaded ZIP to blob"
        
        return $jobArmId
    }
    catch {
        Write-LogToBlob "Failed to start artifact import: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Wait-ImportJobCompletion {
    param(
        [string]$JobArmId,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Waiting for import job completion"
    
    try {
        $apiVersionOffAzure = "2020-01-01"
        $jobUrl = "https://management.azure.com$JobArmId?api-version=$apiVersionOffAzure"
        $waitTimeSeconds = 20
        $maxAttempts = 50 * (60 / $waitTimeSeconds)  # 50 minutes timeout
        $attempt = 0
        $jobCompleted = $false
     
        do {
            $jobStatus = Invoke-RestMethod -Uri $jobUrl -Method GET -Headers $Headers
            $jobResult = $jobStatus.properties.jobResult
            Write-LogToBlob "Attempt $($attempt + 1): Job status - $jobResult"

            if ($jobResult -eq "Completed") {
                $jobCompleted = $true
                break
            }
            elseif ($jobResult -eq "Failed") {
                Write-LogToBlob "Import job failed" "ERROR"
                throw "Import job failed."
            }
     
            Start-Sleep -Seconds $waitTimeSeconds
            $attempt++
        } while ($attempt -lt $maxAttempts)
     
        if (-not $jobCompleted) {
            Write-LogToBlob "Timed out waiting for import job to complete" "ERROR"
            throw "Timed out waiting for import job to complete."
        }
        else {
            Write-LogToBlob "Import job completed successfully. Machines imported."
        }
    }
    catch {
        Write-LogToBlob "Failed while waiting for import job completion: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   VMWARE COLLECTOR FUNCTIONS   #######
######################################################

function Get-VMwareCollectorAgentId {
    param(
        [string]$EnvironmentName,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Getting VMware Collector Agent ID"
    
    try {
        $subscriptionId = (Get-AzContext).Subscription.Id
        $resourceGroupName = if ($SkillableEnvironment) { "on-prem" } else { "${EnvironmentName}-rg" }
        $apiVersionOffAzure = "2020-01-01"
        
        $vmwareSiteUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.OffAzure/VMwareSites/${EnvironmentName}vmwaresite?api-version=$apiVersionOffAzure"
        $vmwareSiteResponse = Invoke-RestMethod -Uri $vmwareSiteUri -Method GET -Headers $Headers
        
        Write-LogToBlob "VMware Site Response received"
        Write-LogToBlob "$($vmwareSiteResponse | ConvertTo-Json -Depth 10)"
        
        $agentId = $vmwareSiteResponse.properties.agentDetails.id
        Write-LogToBlob "Agent ID extracted: $agentId"
        
        return $agentId
    }
    catch {
        Write-LogToBlob "Failed to get VMware Collector Agent ID: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Invoke-VMwareCollectorSync {
    param(
        [string]$AgentId,
        [string]$EnvironmentName,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Synchronizing VMware Collector"
    
    try {
        $subscriptionId = (Get-AzContext).Subscription.Id
        $resourceGroupName = if ($SkillableEnvironment) { "on-prem" } else { "${EnvironmentName}-rg" }
        $assessmentProjectName = "${EnvironmentName}asmproject"
        $vmwareCollectorName = "${EnvironmentName}vmwaresitevmwarecollector"
        
        $vmwareCollectorUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Migrate/assessmentprojects/$assessmentProjectName/vmwarecollectors/$vmwareCollectorName?api-version=2018-06-30-preview"
        Write-LogToBlob "VMware Collector URI: $vmwareCollectorUri"
        
        $vmwareCollectorBody = @{
            "id"         = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Migrate/assessmentprojects/$assessmentProjectName/vmwarecollectors/$vmwareCollectorName"
            "name"       = "$vmwareCollectorName"
            "type"       = "Microsoft.Migrate/assessmentprojects/vmwarecollectors"
            "properties" = @{
                "agentProperties" = @{
                    "id"               = "$AgentId"
                    "lastHeartbeatUtc" = "2025-04-24T09:48:04.3893222Z"
                    "spnDetails"       = @{
                        "authority"     = "authority"
                        "applicationId" = "appId"
                        "audience"      = "audience"
                        "objectId"      = "objectid"
                        "tenantId"      = "tenantid"
                    }
                }
                "discoverySiteId" = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.OffAzure/VMwareSites/${EnvironmentName}vmwaresite"
            }
        } | ConvertTo-Json -Depth 10
        
        Write-LogToBlob "VMware Collector Body:"
        Write-LogToBlob "$vmwareCollectorBody"

        $response = Invoke-RestMethod -Uri $vmwareCollectorUri `
            -Method PUT `
            -Headers $Headers `
            -ContentType 'application/json' `
            -Body $vmwareCollectorBody

        Write-LogToBlob "VMware Collector sync response:"
        Write-LogToBlob "$($response | ConvertTo-Json -Depth 10)"
        
        Write-LogToBlob "VMware Collector synchronized successfully"
    }
    catch {
        Write-LogToBlob "Failed to synchronize VMware Collector: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   ASSESSMENT FUNCTIONS   #############
######################################################

function New-MigrationAssessment {
    param(
        [string]$EnvironmentName,
        [hashtable]$Headers
    )
    
    Write-LogToBlob "Creating migration assessment"
    
    try {
        $subscriptionId = (Get-AzContext).Subscription.Id
        $resourceGroupName = if ($SkillableEnvironment) { "on-prem" } else { "${EnvironmentName}-rg" }
        $assessmentProjectName = "${EnvironmentName}asmproject"
        
        $assessmentBody = @{
            "type" = "Microsoft.Migrate/assessmentprojects/assessments"
            "apiVersion" = "2024-03-03-preview"
            "name" = "$assessmentProjectName/assessment2"
            "location" = "swedencentral"
            "tags" = @{}
            "kind" = "Migrate"
            "properties" = @{
                "settings" = @{
                    "performanceData" = @{
                        "timeRange" = "Day"
                        "percentile" = "Percentile95"
                    }
                    "scalingFactor" = 1
                    "azureSecurityOfferingType" = "MDC"
                    "azureHybridUseBenefit" = "Yes"
                    "linuxAzureHybridUseBenefit" = "Yes"
                    "savingsSettings" = @{
                        "savingsOptions" = "RI3Year"
                    }
                    "billingSettings" = @{
                        "licensingProgram" = "Retail"
                        "subscriptionId" = "$subscriptionId"
                    }
                    "azureDiskTypes" = @()
                    "azureLocation" = "swedencentral"
                    "azureVmFamilies" = @()
                    "environmentType" = "Production"
                    "currency" = "USD"
                    "discountPercentage" = 0
                    "sizingCriterion" = "PerformanceBased"
                    "azurePricingTier" = "Standard"
                    "azureStorageRedundancy" = "LocallyRedundant"
                    "vmUptime" = @{
                        "daysPerMonth" = "31"
                        "hoursPerDay" = "24"
                    }
                }
                "details" = @{}
                "scope" = @{
                    "azureResourceGraphQuery" = @"
migrateresources
| where id contains "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.OffAzure/vmwareSites/${EnvironmentName}vmwaresite"
"@
                    "scopeType" = "AzureResourceGraphQuery"
                }
            }
        } | ConvertTo-Json -Depth 10

        $assessmentUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.Migrate/assessmentProjects/$assessmentProjectName/assessments/assessment2?api-version=2024-03-03-preview"
        
        Write-LogToBlob "Assessment URI: $assessmentUri"
        Write-LogToBlob "Assessment Body: $assessmentBody"
        
        $response = Invoke-RestMethod `
            -Uri $assessmentUri `
            -Method PUT `
            -Headers $Headers `
            -ContentType 'application/json' `
            -Body $assessmentBody

        Write-LogToBlob "Assessment created successfully"
        Write-LogToBlob "Assessment response: $($response | ConvertTo-Json -Depth 10)"
    }
    catch {
        Write-LogToBlob "Failed to create migration assessment: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   MAIN EXECUTION FUNCTION   ##########
######################################################

function Invoke-AzureMigrateConfiguration {
    param(
        [string]$EnvironmentName
    )
    
    Write-LogToBlob "=== Starting Azure Migrate Configuration ==="
    Write-LogToBlob "Environment: $EnvironmentName"
    Write-LogToBlob "Skillable Mode: $SkillableEnvironment"
    
    try {
        # Step 1: Initialize modules and logging
        Import-AzureModules
        Initialize-LogBlob
        
        # Step 2: Create Azure environment (skip if Skillable)
        if (-not $SkillableEnvironment) {
            New-AzureEnvironment -EnvironmentName $EnvironmentName
        }
        
        # Step 3: Get authentication headers
        $headers = Get-AuthenticationHeaders
        
        # Step 4: Register Azure Migrate tools
        Register-MigrateTools -EnvironmentName $EnvironmentName -Headers $headers
        
        # Step 5: Download and import discovery artifacts
        $localZipPath = Get-DiscoveryArtifacts
        $jobArmId = Start-ArtifactImport -LocalZipFilePath $localZipPath -EnvironmentName $EnvironmentName -Headers $headers
        Wait-ImportJobCompletion -JobArmId $jobArmId -Headers $headers
        
        # Step 6: Configure VMware Collector
        $agentId = Get-VMwareCollectorAgentId -EnvironmentName $EnvironmentName -Headers $headers
        Invoke-VMwareCollectorSync -AgentId $agentId -EnvironmentName $EnvironmentName -Headers $headers
        
        # Step 7: Create migration assessment
        New-MigrationAssessment -EnvironmentName $EnvironmentName -Headers $headers
        
        Write-LogToBlob "=== Azure Migrate Configuration Completed Successfully ==="
    }
    catch {
        Write-LogToBlob "=== Azure Migrate Configuration Failed ===" "ERROR"
        Write-LogToBlob "Error: $($_.Exception.Message)" "ERROR"
        throw
    }
}

######################################################
##############   SCRIPT EXECUTION   #################
######################################################

# Execute the main function
try {
    Invoke-AzureMigrateConfiguration -EnvironmentName $environmentName
}
catch {
    Write-Host "Script execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}