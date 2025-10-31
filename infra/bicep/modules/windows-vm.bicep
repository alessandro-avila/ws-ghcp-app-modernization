// Default parameters (can be overridden during deployment)
@description('The name of the virtual machine')
param name string = 'crgar-migr-vm'

@description('The location for the virtual machine')
param location string = 'swedencentral'

@description('The size of the virtual machine')
param vmSize string = 'Standard_E16_v5'

@description('Network interface IDs to attach')
param networkInterfaceIds array

@description('OS disk name')
param osDiskName string = 'crgar-migr-vm-os'

@description('OS disk storage account type')
param osDiskStorageAccountType string = 'Standard_LRS'

@description('Data disk configuration')
param dataDisks array = [
  {
    name: 'crgar-migr-vm-disk1'
    createOption: 'Empty'
    diskSizeGB: 1024
    storageAccountType: 'Standard_LRS'
    caching: 'ReadOnly'
  }
]

@description('Boot diagnostics storage account URI')
param bootDiagnosticsStorageUri string = ''

@description('DSC extension configuration')
param dscConfiguration object = {}

@description('Custom script extension configuration')
param customScriptExtension object = {}

@description('Tags to apply to the virtual machine')
param tags object = {}

resource vm 'Microsoft.Compute/virtualMachines@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    securityProfile: {
      securityType: 'Standard'
    }
    storageProfile: {
      imageReference: {
        communityGalleryImageId: '/CommunityGalleries/crgardemogallery-3167aeab-db3a-49f3-a41b-52b828f29a3c/Images/migrate-lab-0.0.4/Versions/0.0.4'
      }
      osDisk: {
        name: osDiskName
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: osDiskStorageAccountType
        }
        caching: 'ReadWrite'
      }
      dataDisks: [for disk in dataDisks: {
        lun: disk.lun
        name: disk.name
        createOption: disk.createOption
        diskSizeGB: contains(disk, 'diskSizeGB') ? disk.diskSizeGB : null
        managedDisk: contains(disk, 'storageAccountType') ? {
          storageAccountType: disk.storageAccountType
        } : null
        caching: disk.caching
      }]
    }
    networkProfile: {
      networkInterfaces: [for (nicId, i) in networkInterfaceIds: {
        id: nicId
        properties: {
          primary: i == 0
        }
      }]
    }
    diagnosticsProfile: !empty(bootDiagnosticsStorageUri) ? {
      bootDiagnostics: {
        enabled: true
        storageUri: bootDiagnosticsStorageUri
      }
    } : null
  }
}

resource dscExtension 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = if (!empty(dscConfiguration)) {
  parent: vm
  name: 'InstallWindowsFeatures'
  location: location
  properties: {
    publisher: 'Microsoft.Powershell'
    type: 'DSC'
    typeHandlerVersion: '2.77'
    autoUpgradeMinorVersion: true
    settings: dscConfiguration
  }
}

resource customScript 'Microsoft.Compute/virtualMachines/extensions@2023-09-01' = if (!empty(customScriptExtension)) {
  parent: vm
  name: '${name}-vmext-hyperv'
  location: location
  properties: {
    publisher: 'Microsoft.Compute'
    type: 'CustomScriptExtension'
    typeHandlerVersion: '1.9'
    settings: customScriptExtension
  }
  dependsOn: [
    dscExtension
  ]
}

output vmName string = vm.name
output vmId string = vm.id
