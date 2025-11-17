@lab.Title

## Welcome @lab.User.FirstName! 

Let's familiarize yourself with the lab environment.
At the top you will have two tabs: **Instructions** and **Resources**

In Resources you will find useful information like credentials and links. You can switch between them at any time.


Now, let's begin. Log into the virtual machine using the following credentials: <br>
Username: +++@lab.VirtualMachine(Win11-Pro-Base).Username+++ <br>
Password: +++@lab.VirtualMachine(Win11-Pro-Base).Password+++

===

# Lab Overview: What are we going to do today?

The objective of this lab is to explore the different steps involved in a real-life migration

---

#### Exercise 1: Prepare a migration:
* Learn how to install an appliance that collects data from an on-premises datacenter using Azure Migrate

---

#### Exercise 2: Analyze migration data and build a business case:
* Learn how to build a Business Case and decide on the next step when planning a migration

---

#### Exercise 3: Migrate a .NET application:
* Modernize a .NET application using GitHub Copilot and deploy it to Azure.

---

#### Exercise 4: Migrate a Java application:
* Modernize a Java application using GitHub Copilot.


Each exercise is independent. If you get stuck in any of them, you can proceed to the next one

===
# Exercise 1: Prepare a migration

**Goals:**
- Understand how Azure Migrate works in a simulated datacenter environment
- Learn to create and configure an Azure Migrate project
- Install and configure an Azure Migrate appliance for data collection
- Connect the appliance to your on-premises environment for discovery
- Validate that the appliance is successfully collecting data from your infrastructure

## Overview
Migration preparation is the foundation of any successful cloud migration. This exercise teaches you how to set up Azure Migrate to discover and assess your on-premises environment. You'll work with a simulated datacenter containing multiple VMs running different applications.

Click Next to start the exercise

===
### Understand our lab environment

The lab simulates a datacenter by having a VM hosting server with several VMs inside simulating different applications

1. [ ] Open Edge and navigate to the Azure Portal using the following link. This link enables some preview features we will need later: ++https://aka.ms/migrate/disconnectedAppliance++
1. [ ] Login using the credentials in the Resources tab. Instead of using the Password, you may be requested to use the Temporary Access Password (TAP)

> [+Hint] Trouble finding the Resources tab?
>
> Navigate to the top right corner of this screen where you can always find the credentials and important information
> ![text to display](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0010.png)

1. [ ] Open the list of resource groups. You will find one called `on-prem`
2. [ ] Explore the resource group. Find a VM called `lab@lab.LabInstance.Id-vm`
3. [ ] Open the VM. Click **Connect**. Wait until the page is fully loaded
4. [ ] Click **Download RDP file**, wait until the download completes and open it
5. [ ] Login to the VM using the credentials
    1. [ ] Username: `adminuser`
    2. [ ] Password: `demo!pass123`


You have now logged into your on-premises server!
Let's explore what's inside in the next chapter

===
### Understand our lab environment: The VM

This virtual machine represents an on-premises server.
It has nested virtualization. Inside you will find several VMs.

In the Windows menu, open the `Hyper-V Manager` to discover the inner VMs.

> [+Hint] How to open Hyper-V manager
>
> Open the **Server Manager** from the Windows menu. Select **Hyper-V**, right click in your server and click in **Hyper-V Manager**
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00915.png)

1. [ ] In **Hyper-V Manager**, select the inner VM **WEB-2025-100**. Right click ->  **Connect**

> [+Hint] Connecting to a Hyper-V VM from Hyper-V host
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0012.png)

3. [ ] If you are required to sign in, use the following credentials<br>
	username: +++adminuser+++<br>
    password: +++demo!pass123+++
4. [ ] Open Edge inside the Hyper-V VMand verify that an application is running by opening +++http://172.100.2.110+++<br>
	Notice that the first time you run it. It will take some time to load

> [+Hint] Open an application in Hyper-V Guest VM
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0013.png)

We will now create another VM and install the Azure Migrate Appliance

===
### Create Azure Migrate Project
Let's now create an Azure Migrate project 

1. [ ] For the next few steps you will complete them on the Skillable VM, *not* the Hyper-V machine you opened in the previous activity.  You can hit the minimize button in the Hyper-V machine to get back to your Skillable VM.
2. [ ] Open Edge, launch the Azure Portal and in the search bar look for +++Azure Migrate+++
3. [ ] Click **Create Project**
4. [ ] Use the existing Resource Group: +++on-prem+++
5. [ ] Enter a project name. For example +++migrate-prj+++
6. [ ] Select a region. For example +++Canada+++


===
### Download the Appliance

The **Azure Migrate appliance** is a preconfigured virtual machine that runs in your on‑premises environment. 
It securely connects to Azure, discovers your servers, collects performance and configuration data, and sends that data to your Azure Migrate project so you can plan and right‑size your migration.

In the next steps you will download the appliance image that will run inside your Hyper‑V host. Later, you will deploy it as a new VM, connect it to your Azure Migrate project using the project key you generate, and let it start discovering the VMs in this lab environment.

1. [ ] Once in the Azure Migrate Project portal
1. [ ] Select **Start discovery** -> **Using appliance** -> **For Azure**

    > [+Hint] Screenshot
    >
    > ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0090.png)

===
### Download the Appliance

In the Discover page

> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0091.png)

1. [ ] Select **Yes, with Hyper-V** in the dropdown
1. [ ] Enter a name for the appliance. For example +++lab-appliance+++ and click **Generate key**
1. [ ] Take note of the **Project key**. You cannot retrieve it again.
       You can store it here: @lab.TextBox(MigrateApplianceKey)

1. [ ] Select **VHD file**    
2. [ ] You need to download the appliance VHD **inside your on-premises server**. 

	Copy the download link by right-clicking the **Download** button and clicking **Copy Link**. 

===
### Extract the Appliance

1. [ ] Go back to the Hyper-V host VM. This is where you have the Hyper-V Manager with the list of all VMs.

3. [ ] Open the browser and paste the link. This will download the VHD. 
	
    ***Make sure you are doing this inside the Hyper-V VM!*** You can also use this link: +++https://go.microsoft.com/fwlink/?linkid=2191848+++

6. [ ] Copy the downloaded file to the F drive and extract its contents

> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00914.png)

===

### Install the Appliance
1. [ ] Open the Hyper-V manager
    > [+Hint] How to open Hyper-V manager
    >
    > Open the **Server Manager** from the Windows menu. Select **Hyper-V**, right click in your server and click in **Hyper-V Manager**
    >
    > ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00915.png)

1. [ ] Select **New** -> **Virtual Machine**
    > [+Hint] Create virtual machine
    >
	> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0092.png)

1. [ ] Click **Next** and enter a name. For example, +++AZMAppliance+++
1. [ ] Click **Store the virtual machine in a different location** and specify +++F:\Hyper-V\Virtual Machines\appliance+++
1. [ ] Use **Generation 1** and click **Next**
1. [ ] Select +++16384+++ MB of RAM and click **Next**
1. [ ] In **Connection**, select ++NestedSwitch++ and click **Next**
1. [ ] Select **Use an existing virtual hard drive** 
1. [ ] Click **Browse** and look for the extracted zip file on the **F:\\** drive.

    > [+Hint] Create virtual machine
    >
	> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00925.png)

1. [ ] Click **Finish** and start the new VM by right-clicking and selecting **Start** 
1. [ ] Double-click the VM to open a Remote Desktop connection to it. Initially, it will have a black screen for several minutes until it starts

You now have an appliance up and running on your server. This appliance will scan all the VMs and collect all needed information to plan a migration. Now, we need to configure the appliance so it can run a scan on the environment


===

### Connect to the appliance

We will now configure the appliance.

1. [ ] Start by accepting the license terms
1. [ ] Assign a password for the appliance. Use +++Demo!pass123+++
1. [ ] Send a **Ctrl+Alt+Del** command and log into the VM

	> [+Hint] Do you know how to send Ctrl+Alt+Del to a VM?
  	>
  	> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0093.png)

===

### Connect the appliance to Azure Migrate

Once we log in, the machine will configure itself. Wait until the browser displays the Azure Migrate Appliance Configuration. This will take about 4 minutes

1. [ ] Agree to the terms of use and wait until it checks connectivity to Azure
	> [+Hint] Screenshot
  	>
  	> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00932.png)

1. [ ] Paste the key we obtained while creating the appliance in the Azure Portal and click **Verify**. 

   During this process, the appliance software is updated. This will take several minutes and will require you to **refresh** the page.

    > [+Hint] Your key was: 
    > ++@lab.Variable(MigrateApplianceKey)++


	> [+Hint] If Copy & Paste does not work
  	>
  	> You can type the clipboard in the VM
    > ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0094.png)

1. [ ] Login to Azure. If the **Login** button is grayed out, you need to **Verify** the key from the previous step again

    > [+Hint] Hint
    >
	> Remember that the credentials are in the **Resources** tab.
    > ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00945.png)


You have now connected the appliance to your Azure Migrate project. In the next steps we will provide credentials for the appliance to scan your Hyper-V environment

===

### Configure the appliance

Once the appliance finishes registering, you will be able to see it in the Azure Portal, but it still cannot scan your servers because it doesn't have authentication credentials.
We will now provide Hyper-V host credentials. The appliance will use these credentials to scan Hyper-V and discover all servers

1. [ ] In step 2 **Manage credentials and discovery sources**, click **Add credentials**

    username: +++adminuser+++
    
    password: +++demo!pass123+++

    ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00946.png)


1. [ ] Click **Add discovery source** and add the IP address of the Hyper-V host: +++172.100.2.1+++

    > [+Hint] Hint
    >
	>![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00947.png)
    >![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00948.png)

1. [ ] We're almost there! Now we need to add credentials to analyze the software inside the VMs and the databases associated with the applications. Add credentials for Windows (Non-domain), Linux (Non-domain), SQL Server and PostgreSQL Server

	username: +++adminuser+++

    password: +++demo!pass123+++

    > [+Hint] Hint
    >
	>![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00949.png)
    >![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/009491.png)


1. [ ] Click **Start discovery**
1. [ ] Close the VM, we are going back to the Azure portal

===

### Validate the appliance is running
The appliance will start collecting data and sending it to your Azure Migrate project.

1. [ ] Close the virtual machine and go back to the Azure Portal
2. [ ] Search for **Azure Migrate** -> **All projects** and open your project. If you followed the naming guide, it should be called **migrate-prj**
	> [+Hint] Screenshot
    >
	>![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0095.png)

4. [ ] In the left panel, find **Manage** -> **Appliance** and open the appliance you configured.

	> [+Hint] Screenshot
    >
    >![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00951.png)


4. [ ] Validate that all services are running.
	> [+Hint] Screenshot
    >
    >![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/00952.png)

===

Performing an assessment takes time. To get data we need to run the appliance for hours.

In real-life scenarios, it's better to keep it running for several days. This way Azure Migrate will have a better understanding of application resource requirements and will be able to provide better sizing recommendations.

Since we don't have all this time now, we have prepared another Azure Migrate project with data already populated for you.

===

# Exercise 1 Summary

**What you accomplished:**

1. ✅ **Environment Setup:** Explored a simulated datacenter environment with nested VMs representing business applications
2. ✅ **Azure Migrate Project:** Created a centralized hub for organizing migration activities and storing discovery data
3. ✅ **Appliance Installation:** Downloaded and deployed the Azure Migrate appliance on your on-premises infrastructure
4. ✅ **Appliance Configuration:** Connected the appliance to Azure and configured credentials for automated discovery
5. ✅ **Data Collection:** Started the continuous discovery process that gathers performance and dependency data

**Key Takeaways:**
- The Azure Migrate appliance is the foundation of any migration assessment
- Proper credential configuration is essential for comprehensive data collection
- Discovery takes time - longer collection periods provide more accurate sizing recommendations
- The appliance runs continuously to capture performance patterns and dependencies

**Next Steps:**
With data collection underway, you're ready to analyze the gathered information and build a migration strategy. In Exercise 2, you'll learn how to turn raw discovery data into actionable migration plans.

===

# Exercise 2: Analyze migration data and build a business case

**Goals:**
- Understand how to review and clean migration data for accuracy
- Learn to group VMs into applications for better migration planning
- Create and analyze a business case to justify migration investments
- Evaluate technical readiness and migration strategies using assessments

## Overview
Your appliance has been collecting data from the on-premises environment. Now it's time to analyze this data and make migration decisions. You'll clean the data, group VMs into applications, build a business case, and perform technical assessments.

1. [ ] Go to the Azure Portal and open the already prepared project: ++lab@lab.LabInstance.Id-azm++

![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0095.png)

===
# Prepare your data

Your appliance collects data continuously, but some VMs may have issues that need attention.

1. [ ] Open the Azure Migrate project overview
2. [ ] Open the **Action center** blade from the panel on the left

Common issues you'll see:
- VMs that are powered off
- Connection failures due to wrong credentials  
- Missing performance data

In production, fix these issues for better accuracy. For this lab, we'll work with what we have.

> [+Hint] Screenshot
>
>![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01005.png)

===
# Create Applications

Group related VMs into applications so they migrate together. This prevents breaking dependencies between components.

Let's create an application for Contoso University:

1. [ ] Expand **Explore applications** in the left panel
2. [ ] Open the **Applications** page
3. [ ] Click **Define application** -> **New application**
4. [ ] In **Name**, enter +++ContosoUniversity+++
5. [ ] In **Type**, select **Custom** (we have source code access)
6. [ ] In **Workloads**, click in **Link Workloads**
7. [ ] In the Filter search bar, type +++ContosoUniversity+++ and select all results and click **Next**

> [+Hint] Screenshot
>
>![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01002.png)

> [!Knowledge] Important: What is a **workload** in Azure Migrate?
>
> A workload is a distinct application or component that can be discovered, assessed, and migrated independently.
>
>
> For example, if a single VM hosts:
> - the **VM** itself
> - a **SQL database**
> - a **Tomcat web application**
>
> then there are **three workloads** on that VM (the VM, the database, and the web application). Each workload can follow its own migration path and target Azure service.
>
> When you migrate to Azure PaaS, the set of workloads in the **current environment** and the **target environment** may differ. Continuing the same example, you might migrate the SQL database to **Azure SQL Database** and the Tomcat web application to **Azure App Service**, while the original VM is decommissioned. In that target state, you now have **two workloads** (the database and the web application), even though you started with three on the on-premises VM.
    

7. [ ] In **Properties**, select any criticality and complexity, then click in **Review + Create** and then **Create**

You might receive an error message like the one bellow. This is only happening because we are using Preview features from the Azure Portal. Dismiss the error message and continue to the next section

![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0014.png)



===
# Build a Business Case

A business case shows the financial value of migrating to Azure. It calculates costs, savings, and environmental benefits.

1. [ ] In **Decide and plan**, open **Business cases**
2. [ ] Click **Build business case**

Create a targeted business case for Contoso University:

3. [ ] Name: +++contosouniversity+++
4. [ ] Select **Selected Scope**
5. [ ] Click **Add applications**
6. [ ] Select **ContosoUniversity**, then click **Next**
7. [ ] Choose **West US 2** as target region
8. [ ] Add **15%** Azure discount 
9. [ ] Click **Build business case**

This takes several minutes to calculate. Let's examine a completed example while we wait.

===
# Analyze an Existing Business Case

Let's examine a completed business case that covers the entire datacenter with a modernization focus.

1. [ ] Expand **Decide and Plan** and open **Business cases**
2. [ ] Open **businesscase-for-paas**

## Key Metrics
Notice the overview metrics:
- **Cost comparison:** On-premises vs Azure costs
- **Infrastructure scope:** 40 VMs (13 Linux, 27 Windows), 22 web apps, 6 databases  
- **Annual savings:** $212.3K potential savings

Hover over the info icons to learn how costs are calculated.

1. [ ] Open **Business Case Reports** -> **Current on-premises vs future**
2. [ ] Review the cost breakdown table
3. [ ] Scroll down to see CO₂ emissions reduction estimates

===
# Migration Strategies

1. [ ] Navigate to **Migration Strategies**

This shows Gartner's 6R migration approaches:
- **Rehost:** Lift-and-shift to Azure VMs
- **Replatform:** Move to managed services (Azure SQL MI)
- **Refactor:** Modernize to PaaS (App Service)

The report recommends a mix of rehosting and replatforming for your workloads.
Some applications will be replatformed, some others can only be rehosted. The total cost is the sum of all values

Can you spot:
1. [ ] Total cost for all Azure applications?
2. [ ] Cost for applications hosted on VMs?
3. [ ] Cost for SQL databases on VMs?

===
# Azure Cost Assumptions

1. [ ] Expand **Settings** -> **Azure cost**

Here you can fine-tune estimates by adjusting:
- Target regions
- Discount percentages
- Migration timeline  
- Performance buffers

> [!Alert] Changing these settings now will trigger a full recalculation, which takes too long for this exercise.
> For the moment, leave the default values as they are. Once you finish the exercise, you are welcome to come back, adjust the settings, and examine how the results change.
>

> Learn more in the [official documentation](https://learn.microsoft.com/en-us/azure/migrate/concepts-business-case-calculation?view=migrate)

===
# Technical Assessment

While business cases show financial impact, **Assessments** evaluate technical readiness. They help choose the right migration approach and Azure services for each workload.

When you create a business case, an assessment is automatically created.

1. [ ] Expand **Decide and plan** -> **Assessments**


> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01007.png)

===
# Review Assessment Details

1. [ ] Open **businesscase-businesscase-for-paas** assessment  
2. [ ] Notice the **Recommended path**: "PaaS preferred"

Can you spot:
3. [ ] Monthly costs for this approach?
4. [ ] What percentage of VMs will be **Replatformed** vs **Rehosted**?
5. [ ] Cost breakdown: Storage, Security, and Compute?

6. [ ] Click **View Details** or select the **PaaS preferred** tab


> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01008.png)

You will see three migration targets:
- Web apps -> App Service
- SQL Instances -> Azure VMs  
- Servers -> Azure VMs

7. [ ] Open **Web apps to App Service**

===
# Web Apps Analysis

Can you spot:
1. [ ] Total number of applications?
2. [ ] Applications "Ready with conditions"?
3. [ ] Recommended App Service Plan?
4. [ ] Top migration issues to address?

From the application list, select **ContosoUniversity** to see detailed analysis.

Can you spot:
5. [ ] Application URL?
6. [ ] Migration warnings (check Readiness tab)?

This application needs some work but is a good migration candidate.

===
# Application Deep Dive

1. [ ] Go back to the main page
2. [ ] Expand **Explore applications** -> **Applications**
3. [ ] Select **ContosoUniversity**

Can you spot:
4. [ ] How many servers and databases make up the application?
5. [ ] What is the Operating system support status?
6. [ ] Click **Workloads** to see details on the servers that compose this application

===
# Application Deep Dive
In the Workloads page, can you spot:
5. [ ] Which servers are out of support?
6. [ ] Which are in extended support?

7. [ ] Open the out-of-support database by clicking in the name **ContosoUniversity-pgsql-db**

Can you spot:
8. [ ] What version of PostgreSQL is currently running?

===

### Software Inventory
Finally, let's check what other software is running on the servers, to make sure we are not forgetting any important component

9. [ ] Go back to the Worloads, and open **win-ContosoUniversity-backend**


> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01010.png)


Can you spot:
10. [ ] Total software count on the VM?
11. [ ] Number of web hosting software packages?

This analysis helps you understand migration complexity and dependencies.
===

# Exercise 2 Quiz. Question 1

I hope now it is clear how to navigate data, where to find workloads, financial or technical information.

Let's see if you can find the answers to these questions:

@lab.Activity(PowerOffLinuxVms)

> [+Hint] Need some help?
> 
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01001.png)

===

# Exercise 2 Quiz. Question 2

@lab.Activity(CountWin2016)


> [+Hint] Need some help?
> 
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01004.png)

===

# Exercise 2 Quiz. Question 3

@lab.Activity(CostVMsForApps)

> [+Hint] Need some help?
> 
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/01011.png)

===

# Exercise 2 Quiz. Question 4

@lab.Activity(CostSaving)

> [+Hint] Need some help?
> 
> Navigate the the business case `businesscase-for-paas`
> Open the Overview page
> Look at the Potential cost savings card and find the savings

===

# Exercise 2 Quiz. Question 5

@lab.Activity(SecuritySaving)

> [+Hint] Need some help?
> 
> Navigate the the business case `businesscase-for-paas`
>
> On the menu in the left, open `Business Case Reports` and navigate to `Current on-premises vs future`
>
> Look for Security row, and pay attention at the last column

===

# Exercise 2 Summary

**What you accomplished:**

1. ✅ **Data Quality:** Learned to identify and address data collection issues that could impact migration planning
2. ✅ **Application Grouping:** Created logical application definitions to ensure coordinated migration of related components  
3. ✅ **Business Case:** Built financial justification showing potential cost savings, TCO, and ROI for migration
4. ✅ **Technical Assessment:** Evaluated workload readiness and identified optimal Azure services and migration strategies

**Key Takeaways:**
- Clean, accurate data is essential for reliable migration planning
- Business cases provide the financial justification needed to secure migration funding
- Technical assessments ensure your applications are ready for Azure and help choose the right services
- Combining financial and technical analysis gives you a complete migration strategy

**Next Steps:**
You now have the foundation for informed migration decisions. In the following exercises, you'll put this knowledge into practice by modernizing Contoso University using GitHub Copilot.

===

# Exercise 3: .NET App modernization

**Goals:**
- Clone and set up the Contoso University .NET application
- Use GitHub Copilot App Modernization to assess the application for cloud readiness
- Upgrade the application from .NET Framework to .NET 9
- Migrate authentication from Windows AD to Microsoft Entra ID
- Resolve cloud readiness issues and deploy to Azure App Service

## Overview
Before we begin, make sure you are logged into GitHub: [https://github.com/enterprises/skillable-events](https://github.com/enterprises/skillable-events "https://github.com/enterprises/skillable-events")

> [!Knowledge]
> Use the Azure Portal credentials from the resources tab.
> 
> Make sure you don't close the GitHub site. Otherwise GitHub Copilot might not work due to the restrictions of the lab environment.

Let us get our hands dirty on some code.

We want to use GitHub Copilot to modernize our .NET application in Visual Studio.

In Visual Studio we have an extension called *GitHub Copilot app modernization*. This extension uses a dedicated agent inside GitHub Copilot to help you upgrade this project to a newer .NET version and will afterwards support you with the migration to Azure.

With this extension you can:

* Upgrade to a newer version of .NET
* Migrate technologies and deploy to Azure
* Modernize your .NET app, especially when upgrading from .NET Framework
* Assess your application's code, configuration, and dependencies
* Plan and set up the right Azure resource
* Fix issues and apply best practices for cloud migration
* Validate that your app builds and tests successfully

===

# 3.1 Clone the repository

The first application we will migrate is *Contoso University*.

Open the following [link to the repository](https://github.com/crgarcia12/migrate-modernize-lab "link to the repository").

In the repository view click on *<> Code* and in the tab *Local* choose *HTTPS* and *Copy URL to clipboard*.
> The URL should look like this: *https://github.com/crgarcia12/migrate-modernize-lab.git*

 !IMAGE[Screenshot 2025-11-14 at 10.42.04.png](instructions310257/Screenshot 2025-11-14 at 10.42.04.png)

1. Open Visual Studio  
2. Select *Clone a repository*  
3. Paste the repository link in the *Repository Location*  
4. Click *Clone* and wait a moment for the cloning to finish
5. Let us check out the Contoso University project. In the *View* menu select *Solution Explorer*. Then right click on the *ContosoUniversity* project and select *Rebuild*

It is not required for the lab, but if you want you can run the app in IIS Express (Microsoft Edge).

!IMAGE[0030.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0030.png)

Edge will open and you will see the application running at `https://localhost:44300`

===

# 3.2 Code assessment + app upgrade to .NET 9

The first step is to do a code assessment, followed by a complete upgrade. For that we will use the *GitHub Copilot app modernization* extension.

1. Right click in the project and select *Modernize*

!IMAGE[0040.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0040.png)

> [!Hint] If GitHub Copilot asks you to sign in, click on *Already have an account? Sign in* and follow the steps to sign in to GitHub.

2. The GitHub Copilot Chat window will open. Click on *Upgrade to a newer .NET version*. It will paste this exact message in the chat window. You can modify the prompt if you want to target a specific .NET version (8, 9 or 10).

**Before you send the message, be sure to select Claude Sonnet 4.5 as the model to use for this task.**

!IMAGE[Screenshot 2025-11-16 at 17.34.45.png](instructions310257/Screenshot 2025-11-16 at 17.34.45.png)

3. This task will take a while. In case it asks you to allow operations, allow them by answering in the chat window. If it gets stuck, encourage it with nice words to continue. If you have the feeling it gets stuck like it stops working completely, you can a) call your proctors and we help you decide what to do or b) close VS completely and re-open it, but then we need to continue prompting manually.

4. You know when it's done *dotnet-upgrade-report.md* will show up.

===

# 3.3 Deploy the modernized .NET app to Azure

Next we want to deploy our modernized application to Azure App Service.

1. Right click in the project, and select *Modernize* again

!IMAGE[0040.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0040.png)

2. In the GitHub Copilot Chat window, click on *Migrate to Azure*. It will paste this exact message in the chat window. Make sure you send it that GitHub Copilot can start working on the task.

!IMAGE[Screenshot 2025-11-16 at 17.34.45.png](instructions310257/Screenshot 2025-11-16 at 17.34.45.png)

3. While GitHub Copilot is working, let's have a lookt at the assessment report it generated. On the upper right you can either import or export the report. But you can also kick of specific tasks to resolve **Cloud Readiness Issues** here. Exactly what we want to do, because we want to deploy to Azure.

!IMAGE[0080.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0080.png)

4. Begin with clicking on *Migrate from Windows AD to Microsoft Entra ID*. If GitHub Copilot does not pick up tasks automatically, you can always come back to the *dotnet-upgrade-report.md* file and click on the tasks you want to resolve.

5. This may take a while. Ensure all mandatory tasks get resolved.

5. Congratulations! You have successfully modernized the .NET application and deployed it to Azure with the help of GitHub Copilot. Click next to continue.

===

# Exercise 3 Summary

**What you accomplished:**

1. ✅ **Repository Setup:** Cloned the Contoso University repository and validated the .NET application builds successfully
2. ✅ **Cloud Assessment:** Used GitHub Copilot App Modernization to perform comprehensive code analysis for Azure readiness
3. ✅ **Framework Upgrade:** Successfully upgraded the application from legacy .NET Framework to modern .NET 9
4. ✅ **Authentication Modernization:** Migrated from Windows Active Directory to Microsoft Entra ID for cloud-native identity management
5. ✅ **Cloud Readiness:** Resolved all mandatory cloud readiness issues identified in the upgrade report
6. ✅ **Azure Deployment:** Deployed the fully modernized application to Azure App Service

**Key Takeaways:**
- GitHub Copilot App Modernization automates complex upgrade tasks that would take days manually
- The tool generates detailed assessment reports prioritizing issues by urgency (Mandatory, Potential, Optional)
- Authentication modernization is critical for cloud security and integration with Azure services
- Automated validation ensures the upgraded application maintains functionality throughout the process
- The modernization workflow preserves your original code in branches, enabling safe rollback if needed

**Next Steps:**
You've successfully modernized a .NET application for Azure. In Exercise 4, you'll apply similar techniques to modernize a Java application, learning how GitHub Copilot handles different technology stacks.

===

# Exercise 4: Java App modernization

**Goals:**
- Set up the Asset Manager Java application with local development environment
- Use GitHub Copilot App Modernization to perform AppCAT assessment for Azure readiness
- Review and prioritize cloud readiness issues and Java upgrade opportunities
- Execute guided migration tasks to migrate from AWS S3 to Azure Blob Storage
- Validate migration success through automated security and functional testing
- Test the modernized application locally

## Overview
In this exercise, you'll modernize a Java Spring Boot application using GitHub Copilot App Modernization in VS Code. You'll learn how to assess a Java application for cloud readiness, identify migration issues, and automatically remediate them using AI-powered tools. The focus will be on migrating cloud dependencies from AWS to Azure while maintaining application functionality.

**What You'll Do:** Use GitHub Copilot app modernization to assess, remediate, and modernize the Java application in preparation to migrate the workload to App Service

**What You'll Learn:** How GitHub Copilot app modernization works, demonstration of modernizing elements of legacy applications, and the modernization workflow

<!-- Install Java 8: [Download Java 8](https://adoptium.net/download?link=https%3A%2F%2Fgithub.com%2Fadoptium%2Ftemurin8-binaries%2Freleases%2Fdownload%2Fjdk8u472-b08%2FOpenJDK8U-jdk_x64_windows_hotspot_8u472b08.msi&vendor=Adoptium)

Open Docker Desktop

Install Scoop from here:
```
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
Invoke-RestMethod -Uri https://get.scoop.sh | Invoke-Expression
```

Install Maven:
```
scoop install main/maven
```

Clone the repo

Install Github copilot app modernization extension

Open a terminal as Administrator
Run the app ```scripts\startapp.cmd```

Open VSCode, doing `code .`
Open the GH Copilot for App Modernization extension from VSCode

Sign in to Github Copilot
Authorize Visual Studio Code

Make sure you have selected Sonnet 4.5

Click in Upgrade Java Runtime & Frameworks

=== -->

===

# Preparation

1. [ ] Open the browser and navigate to +++https://github.com/enterprises/skillable-events/+++
2. [ ] Authenticate with your Azure credentials

> [+Hint] How to find your Azure credentials
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0010.png)

4. [ ] Open **Docker Desktop** from the windows menu. Wait until the Docker desktop screen shows up
5. [ ] Open the **Terminal**, and run 

```
mkdir C:\gitrepos\lab
cd  C:\gitrepos\lab
git clone https://github.com/crgarcia12/migrate-modernize-lab.git
cd .\migrate-modernize-lab\src\AssetManager\
code .
exit
```
This script will clone a repo with the Java application, and open VS code on that path.

1. [ ] Login to GitHub Enterprise form VS Code.

> [+Hint] How to login to GitHub from VSCode
>
> In visual studio, click on the copilot icon on the top, next to the search bar. Then click in Continue with GitHub. Follow the instructions in the browser
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02003.png)


Next let's begin our modernization work. 

===
Let's first run the application.

1. [ ] Go to the menu **View** -> **Terminal** 
1. [ ] If the **pwsh** terminal is missing, click the + symbol and select **New Terminal**` as shown in the picture bellow

> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02002.png)

1. [ ] In the terminal window, run ```scripts\startapp.cmd```


The first time you run the applications will take some time, because Docker will pull diferent container images like RabbitMQ and Postgres. <br>

If you are asked to grant permissions for Docker, Java or other applications to get Internet or private network access, allow it.

Once both console windows show messages that the apps are running, you should be able to open the browser and navigate to  +++http://localhost:8080+++

 ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02004.png)

You can now close the application, by closing the console windows that were open.

===

1. Open the application in Visual Studio Code
2. Ensure that Claude Sonnet 4.5 is selected as the model in the GitHub Copilot chat pane.
3. Select  `GitHub Copilot app modernization` extension in the Activity bar on the left
4. Navigate the Extension Interface and click **Migrate to Azure** to begin the modernization process.
	!IMAGE[screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02001.png)

<!-- 1. Allow the GitHub Copilot app modernization to sign in to GitHub 
	!IMAGE[ghcp-allow-signin.png](instructions310381/ghcp-allow-signin.png)

1. Authorize your user to sign in

	!IMAGE[gh-auth-user.png](instructions310381/gh-auth-user.png)

1. And finally, authorized it again on this screen

	!IMAGE[gh-auth-screen.png](instructions310381/gh-auth-screen.png)

1. The assessment will start now. Notice that GitHub will install the AppCAT CLI for Java. This might take a few minutes

	!IMAGE[appcat-install.png](instructions310381/appcat-install.png) -->

> [!hint] You can follow the progress of the upgrade by looking at the Terminal in vscode
!IMAGE[assessment-rules.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02005.png)

<!-- Also note that you might be prompted to allow access to the language models provided by GitHub Copilot Chat. Click on **Allow**

!IMAGE[ghcp-allow-llm.png](instructions310381/ghcp-allow-llm.png) -->

---

### Overview of the Assessment

Assessment results are consumed by GitHub Copilot App Modernization (AppCAT). AppCAT examines the scan findings and produces targeted modernization recommendations to prepare the application for containerization and migration to Azure.

- target: the desired runtime or Azure compute service you plan to move the app to.
- mode: the analysis depth AppCAT should use.

**Analysis targets**

Target values select the rule sets and guidance AppCAT will apply.

| Target | Description |
|--------|---------|
| azure-aks | Guidance and best practices for deploying to Azure Kubernetes Service (AKS). |
| azure-appservice | Guidance and best practices for deploying to Azure App Service. |
| azure-container-apps | Guidance and best practices for deploying to Azure Container Apps. |
| cloud-readiness | General recommendations to make the app "cloud-ready" for Azure. |
| linux | Recommendations to make the app Linux-ready (packaging, file paths, runtime details). |
| openjdk11 | Compatibility and runtime recommendations for running Java 8 apps on Java 11. |
| openjdk17 | Compatibility and runtime recommendations for running Java 11 apps on Java 17. |
| openjdk21 | Compatibility and runtime recommendations for running Java 17 apps on Java 21. |

**Analysis modes**

Choose how deep AppCAT should inspect the project.

| Mode | Description |
|--------|---------|
| source-only | Fast analysis that examines source code only. |
| full | Full analysis: inspects source code and scans dependencies (slower, more thorough). |

> [!knowledge]  **Where to change these options**
>
> You can customize this report by editing the file at **.github/appmod-java/appcat/assessment-config.yaml** to change targets and modes.
>
>For this lab, AppCAT runs with the following configuration:
>
>```yaml
>appcat:
>  - target:
>      - azure-aks
>      - azure-appservice
>      - azure-container-apps
>    mode: source-only
>```
>
>If you want a broader scan (including dependency checks) change `mode` to `full`, or add/remove entries under `target` to focus recommendations on a specific runtime or Azure compute service.

===

### Review the Assessment results

After the assessment completes, you'll see a success message in the GitHub Copilot chat summarizing what was accomplished:

!IMAGE[assessment report](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02006.png)

The assessment analyzed the Asset Manager application for cloud migration readiness and identified the following:

Key Findings:

* 9 cloud readiness issues requiring attention
* 4 Java upgrade opportunity for modernization

**Resolution Approach:** More than 78% of the identified issues can be automatically resolved through code and configuration updates using GitHub Copilot's built-in app modernization capabilities.

**Issue Prioritization:** Issues are categorized by urgency level to guide remediation efforts:

* Mandatory (Purple) - Critical issues that must be addressed before migration.
* Potential (Blue) - Performance and optimization opportunities.
* Optional (Gray) - Nice-to-have improvements that can be addressed later.

This prioritization framework ensures teams focus on blocking issues first while identifying opportunities for optimization and future enhancements.

### Review Specific Findings

Click on individual issues in the report to see detailed recommendations. In practice, you would review all recommendations and determine the set that aligns with your migration and modernization goals for the application.

> [!note] For this lab, we will spend our time focusing on one modernization recommendation: updating the code to use Azure Blob Storage instead of AWS S3 buckets.


| Aspect | Details |
|--------|---------|
| **Modernization Lab Focus** | Storage Migration (AWS S3) |
| **What was found** | Migrate from AWS S3 to Azure Blob Storage for scalable and secure storage in Azure |
| **Why this matters** | Migrating an application requires dependencies like storage to migrated alongside the code and data.  |
| **Recommended solution** | Migrate from AWS S3 to Azure Blob Storage |
| **Benefits** | Migrating from S3 to Azure Blob Storage enables seamless integration with other Azure services (like Azure Functions, App Service, and Azure AI), unified identity management through Azure Active Directory, and potential cost savings through features like automated tiering and lifecycle management policies. |

===

### Take Action on Findings

Based on the assessment findings, GitHub Copilot app modernization provides two types of migration actions to assist with modernization opportunities:

1. Using the **guided migrations** ("Run Task" button), which offer fully guided, step-by-step remediation flows for common migration patterns that the tool has been trained to handle. 

2. Using the **unguided migrations** ("Ask Copilot" button), which provide AI assistance with context aware guidance and code suggestions for more complex or custom scenarios.

!IMAGE[4somda23.jpg](instructions310257/4somda23.jpg)

For this workshop, we'll focus on one modernization area that demonstrates how to externalize dependencies in the workload to Azure PaaS before deploying to app service. We'll migrate from AWS S3 buckets to Azure Blob Storage.

===

### Select AWS S3 Task

Begin the modernization by selecting the desired migration task. For our java application, we will migrate to Azure Blob Storage.  You can expand the migration task to see detail about what will happen during the task and an explanation of why it's important to our modernization.

!IMAGE[uoobnjjf.jpg](instructions310257/uoobnjjf.jpg)

### Execute AWS S3 Migration Task

Click the **Run Task** button described in the previous section to kick off the modernization changes needed in the Asset Manager app. This will update the Java code to work Azure Blob Storage instead of AWS S3.

The tool will execute the `appmod-run-task` command for `s3-to-azure-blob-storage` for the Java project, which will examine the workspace structure and initiate the migration task to modernize your java for Azure Blob Storage. If prompted to run shell commands, please review and allow each command as the Agent may require additional context before execution.

### Review Migration Plan and Begin Code Migration

The App Modernization tool has analyzed your java application and generated a comprehensive migration plan in its chat window and in the `plan.md` file. This plan outlines the specific changes needed to implement Azure Blob Storage instead of AWS S3.

!IMAGE[wpoydt5u.jpg](instructions310257/wpoydt5u.jpg)

To Begin Migration type **"Continue"** in the GitHub Agent Chat to start the code refactoring.

===

### Review Migration Process and Progress Tracking

Once you confirm with **"Continue"**, the migration tool begins implementing changes using a structured, two-phase approach designed to ensure traceability and commit changes to a new dedicated code branch for changes to enable rollback if needed.
<!--
**Two-Phase Migration Process:**

> [!knowledge] 
> **Phase 1: Update Dependencies**
- **Purpose**: Add the necessary Azure libraries to your project.
- **Changes made**:
  - Updates `pom.xml` with Azure SDK BOM 
  - Updates `build.gradle` with corresponding Gradle dependencies
  - Adds Spring Cloud Azure version properties.

> [!knowledge] 
> **Phase 2: Configure Application Properties**
- **Purpose**: Update configuration files to use managed identity authentication.
- **Changes made**:
  - Updates `application.properties` to configure PostgreSQL with managed identity (9 lines added, 2 removed)
  - Updates `application-postgres.properties` with Entra ID authentication settings (5 lines added, 4 removed)
  - Replaces username/password authentication with managed identity configuration. -->

**Progress Tracking:**
The `progress.md` file provides real-time visibility into the migration process:
- **Change documentation**: Detailed
-  log of what changes are being made and why.
- **File modifications**: Clear tracking of which files are being updated.
- **Rationale**: Explanation of the reasoning behind each modification.
- **Status updates**: Real-time progress of the migration work.

> [!hint] 
**How to Monitor Progress:**
- Watch the GitHub Copilot chat for real-time status updates
- Check the `progress.md` file in the migration directory for detailed
-  change logs
- Review the `plan.md` file to understand the complete migration strategy
- Monitor the terminal output for any build or dependency resolution messages
> [!hint] 

### Review Migration Completion Summary

Upon successful completion of the validation process, the App Modernization tool presents a comprehensive migration summary report confirming the successful implementation of Azure Blob Storage in your java application.

!IMAGE[ovfbrre3.jpg](instructions310257/ovfbrre3.jpg)

The migration has successfully transformed your application from supporting Amazon S3 storage to instead supporting Azure Blob Storage while maintaining application functionality. The process integrated java Azure dependencies, updated configuration properties, and ensured all validation stages passed including: **CVE scanning, build validation, consistency checks, and test execution**.

**Files Modified:**

The migration process updated the following configuration files:

- `pom.xml` and `build.gradle` - Added Spring Cloud Azure dependencies.

- `application.properties` and `application-postgres.properties` - Configured managed identity authentication.

- Test configurations - Updated to work with the new authentication method

> [!hint] Througout this lab, the GitHub Copilot App Modernization extension will create, edit and change various files. The Agent will give you an option to _Keep_ or _Undo_ these changes which will be saved into a new Branch, preserving your original files in case you need to rollback any changes.
!IMAGE[8fp2nrzs.jpg](instructions310257/8fp2nrzs.jpg)

===

### Validation and Fix Iteration Loop

After implementing the migration changes, the App Modernization tool automatically validates the results through a comprehensive testing process to ensure the migration changes are secure, functional, and consistent.

!IMAGE[y18ydpod.jpg](instructions310257/y18ydpod.jpg)

**Validation Stages:**

| Stage | Validation | Details |
|--------|---------|---------
| 1 | **CVE Validation** | Scans newly added dependencies for known security vulnerabilities.
| 2 | **Build Validation** | Verifies the application compiles and builds successfully after migration changes.
| 3 | **Consistency Validation** | Ensures all configuration files are properly updated and consistent.
| 4 | **Test Validation** | Executes application tests to verify functionality remains intact.

> [!note] During these stages, you might be prompted to allow the **GitHub Copilot app modernization** extension to access GitHub. Allow it and select your user account when asked.
>
>!IMAGE[vflz96jl.jpg](instructions310257/vflz96jl.jpg)

**Automated Error Detection and Resolution:**

The tool includes intelligent error detection capabilities that automatically identify and resolve common issues:

- Parses build output to detect compilation errors.
- Identifies root causes of test failures.
- Applies automated fixes for common migration issues.
- Continues through validation iterations (up to 10 iterations) until the build succeeds.

> [!hint] 
> **User Control:**
> At any point during this validation process, you may interrupt the automated fixes and manually resolve issues if you prefer to handle specific problems yourself. The tool provides clear feedback on what it's attempting to fix and allows you to take control when needed at any time.
>
>This systematic approach ensures your java application is successfully modernized for Azure Blob Storage while maintaining full functionality.
> [!hint] 

===

### Run the App ###

After you have completed the modernization task, run the application to ensure the task was successful:

1. [ ] Go to the menu **View** -> **Terminal** 
1. [ ] If the **pwsh** terminal is missing, click the + symbol and select **New Terminal**` as shown in the picture bellow

> [+Hint] Screenshot
>
> ![Screenshot](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/02002.png)

1. [ ] In the terminal window, run ```scripts\startapp.cmd```

===

### Other Tasks ###

Continue modernization your app by selecting another task and completing the modernization steps.

!IMAGE[tresdc8l.jpg](instructions310257/tresdc8l.jpg)

===

# Exercise 4 Summary

**What you accomplished:**

1. ✅ **Environment Setup:** Configured Java development environment with Docker, Maven, and the Asset Manager application
2. ✅ **Local Validation:** Successfully ran the Asset Manager application locally to establish a working baseline
3. ✅ **AppCAT Assessment:** Performed comprehensive cloud readiness analysis identifying 9 issues and 4 Java upgrade opportunities
4. ✅ **Issue Prioritization:** Learned to distinguish between Mandatory, Potential, and Optional migration issues
5. ✅ **Guided Migration:** Executed automated migration from AWS S3 to Azure Blob Storage using AI-powered code refactoring
6. ✅ **Dependency Updates:** Updated Maven and Gradle configurations with Azure SDK dependencies
7. ✅ **Automated Validation:** Validated migration through CVE scanning, build verification, consistency checks, and test execution
8. ✅ **Post-Migration Testing:** Verified the modernized application runs successfully with Azure Blob Storage

**Key Takeaways:**
- AppCAT provides targeted assessment rules for different Azure services (AKS, App Service, Container Apps)
- GitHub Copilot can automatically resolve over 78% of identified migration issues
- Guided migration tasks offer structured, step-by-step remediation for common patterns
- The tool tracks all changes in `plan.md` and `progress.md` files for complete transparency
- Branch-based workflow enables safe rollback if needed
- Automated validation catches security vulnerabilities and build issues early
- Platform migrations (AWS to Azure) require both code and dependency changes

**Next Steps:**
You've now completed the full migration lifecycle from assessment to deployment. You've learned to use both Azure Migrate for infrastructure planning and GitHub Copilot for application modernization across .NET and Java stacks. These skills prepare you to tackle real-world migration projects with confidence.

**Additional Modernization Opportunities:**
- Continue with other migration tasks identified in the assessment report
- Explore containerization options for deploying to AKS or Azure Container Apps
- Implement additional Azure services like Azure Service Bus (replacing RabbitMQ)
- Apply Java runtime upgrades using the identified opportunities
- Configure managed identities for passwordless authentication

===

## Workshop Recap & What's Next

**Congratulations!** You've successfully completed a comprehensive migration and modernization journey, transforming legacy applications into cloud-ready, secure, and scalable solutions on Azure.

### What You Accomplished

**Exercise 1: Migration Preparation**

- Explored a simulated datacenter environment with nested Hyper-V VMs
- Created and configured an Azure Migrate project for discovery
- Downloaded, installed, and configured the Azure Migrate appliance
- Connected the appliance to on-premises infrastructure with proper credentials
- Initiated continuous discovery for performance and dependency data collection

**Exercise 2: Migration Analysis & Business Case**

- Reviewed and cleaned migration data using Azure Migrate's Action Center
- Grouped related VMs into logical applications (ContosoUniversity)
- Built business cases showing financial justification with cost savings and ROI analysis
- Analyzed technical assessments for cloud readiness and migration strategies
- Evaluated workload readiness across VMs, databases, and web applications
- Navigated migration data to identify issues, costs, and modernization opportunities

**Exercise 3: .NET Application Modernization**

- Cloned and configured the Contoso University .NET application repository
- Used GitHub Copilot App Modernization extension in Visual Studio
- Performed comprehensive code assessment for cloud readiness
- Upgraded application from legacy .NET Framework to .NET 9
- Migrated from Windows AD to Microsoft Entra ID authentication
- Resolved cloud readiness issues identified in the upgrade report
- Deployed the modernized application to Azure App Service

**Exercise 4: Java Application Modernization**

- Set up local Java development environment with Docker and Maven
- Ran the Asset Manager application locally to validate functionality
- Used GitHub Copilot App Modernization extension in VS Code
- Performed AppCAT assessment for Azure migration readiness (9 cloud readiness issues, 4 Java upgrade opportunities)
- Executed guided migration tasks to modernize the application
- Migrated from AWS S3 to Azure Blob Storage with automated code refactoring
- Validated migration success through automated CVE, build, consistency, and test validation
- Tested the modernized application locally

---

### Next Steps & Learning Paths

**Continue Your Azure Journey:**

- [Azure Migrate Documentation](https://learn.microsoft.com/azure/migrate/) - Deep dive into migration tools and strategies
- [Azure Well-Architected Framework](https://learn.microsoft.com/azure/well-architected/) - Learn enterprise architecture best practices
- [GitHub Copilot for Azure](https://learn.microsoft.com/azure/developer/github-copilot/) - Explore AI-powered development tools

**Hands-On Labs:**

- [Azure Migration Center](https://azure.microsoft.com/migration/) - Additional migration resources and tools
- [Azure Architecture Center](https://learn.microsoft.com/azure/architecture/) - Reference architectures and patterns
- [Microsoft Learn - Azure Migration Path](https://learn.microsoft.com/training/paths/migrate-modernize-innovate-azure/) - Structured learning modules

### Key Takeaways

This workshop demonstrated the complete migration lifecycle from discovery to deployment:
- **Assessment First**: Azure Migrate provides comprehensive discovery and financial justification before migration
- **AI-Powered Modernization**: GitHub Copilot dramatically accelerates code modernization while maintaining quality
- **Platform Migration**: Successfully migrated dependencies (S3 to Blob Storage, Windows AD to Entra ID) alongside application code
- **Validation at Every Step**: Automated testing ensures functionality is preserved throughout modernization
