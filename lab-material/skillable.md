@lab.Title

## Hello lab.User.FirstName! 
## Welcome to Your Lab Environment

To begin, log into the virtual machine using the following credentials: 
Username: +++@lab.VirtualMachine(Win11-Pro-Base).Username+++
Password: +++@lab.VirtualMachine(Win11-Pro-Base).Password+++

===
# Lab initial setup
This lab requires some initial setup to ensure that all necessary tools and configurations are in place. Follow the steps below to prepare your environment:

## 1. Upload Azure Migrate Assessment
1. [ ] Open Edge, and head to the Azure Portal using the following link. This link enables some of the preview features we will need later on: +++http://aka.ms/migrate/disconnectedAppliance+++
2. [ ] Login using the credentials in the Resources tab.

    > [+Hint] Troubles finding the resource tab?
    >
    > Navigate to the top right corner of this screen, in there you can always find the credentials and important information
    > ![text to display](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0010.png)

3. [ ] In the search bar, search for ++Azure Migrate++
4. [ ] Open All projects, and open the new project lab@lab.LabInstance.Id-azm
5. [ ] Cick in ++Start discovery++ and select ++Using custom import++
6. [ ] click ++Browse++ and paste the following link 
> ++https://github.com/crgarcia12/migrate-modernize-lab/raw/refs/heads/main/lab-material/Azure-Migrate-Discovery.zip++
> [+Hint] TODO: It seems this only works in Chrome

## 2. Login to GitHub Enterprise
Login to Github enterprise: +++https://github.com/enterprises/skillable-events+++

> [!Knowledge] ATENTION!
>
> Make sure you don't close the GitHub site. Otherwise Copilot might not work

===

# TODO: Lab start: What are we going to do today?

The objective of the lab is to ... TODO

## Part 1: Prepare a migration:
1. An assessment of an on-premises datacenter hyper-v environment using Azure Migrate
1. Building a Business Case and decide on the next step for one application

## Part 2: Migrate an application:
1. Modernize .NET application |using GitHub Copilot app modernization for .NET.
1. Build a pipeline to deploy the application to Azure

Each part is independent.

===
@lab.Activity(Question1)
===
@lab.Activity(Question2)
===
@lab.Activity(Question3)

> [+Hint] Need some help?
> 
> Navigate the the business case `buizzcaseevd`
> Open the Overview page
> Look at the Potential cost savings card and find the savings
===
@lab.Activity(Question4)

> [+Hint] Need some help?
> 
> Navigate the the business case `buizzcaseevd`
>
> On the menu in the left, open `Business Case Reports` and navigate to `Current on-premises vs future`
>
> Look for Security row, and pay attention at the last column

===
@lab.Activity(Question5)

> [+Hint] Need some help?
> 
> Navigate the the business case `buizzcaseevd`
>
> On the menu in the left, open `Business Case Reports` and navigate to `Current on-premises vs future`
>
> Look for Security row, and pay attention at the last column

===
# Part 1: Prepare a migration
### Understand our lab environment

The lab simulates a datacenter, by having a VM hosting server, and several VMs inside simulating different applications

1. [ ] Open Edge, and head to the Azure Portal using the following link. This link enables some of the preview features we will need later on: +++http://aka.ms/migrate/disconnectedAppliance++
1. [ ] Login using the credentials in the Resources tab.

> [+Hint] Troubles finding the resource tab?
>
> Navigate to the top right corner of this screen, in there you can always find the credentials and important information
> ![text to display](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0010.png)

1. [ ] Open the list of resource groups. You will find on called `on-prem`
2. [ ] Explore the resource group. Find a VM called `lab@lab.LabInstance.Id-vm`
3. [ ] Open the VM. Click in `Connect`. Wait until the page is fullo loaded
4. [ ] Click in `Download RDP file` wait until the download is complete and open it
5. [ ] Login to the VM using the credentials
    1. [ ] Username: `adminuser`
    2. [ ] Password: `demo!pass123`


You have now loged in into your on-premises server!
Let's explore whats inside in the next chapter

===
### Understand our lab environment: The VM

This Virtual machine represents an on-premises server.
It has nested virtualization. Inside you will find several VMs.

> ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0020.png)

In the windows menu, open the `Hyper-V Manager` to discover the inner VMs.

TODO: Open one of the vms and see the application running

We will now create another VM, and install the Azure Migrate Appliance

===
### Create Azure Migrate Project
Let's now create an Azure Migrate project

1. [ ] Head back to the Azure Portal, and in the serch bar look for ++Azure Migrate++
2. [ ] Click in ++Create Project++
3. [ ] Create a new Resource Group. You can call it ++migrate-rg++
4. [ ] Enter a project name. For example ++migrate-prj++
5. [ ] Select a region. For example ++Central US++


===
### Download the Appliance
1. [ ] Once the Azure Migrate Project Portal
2. [ ] Select ++Start discovery++ -> ++Using appliance++ -> ++For Azure++
3. [ ] Answer the virtualization question with ++Yes, with Hyper-V++
4. [ ] Select a name for the appliance and click ++Generate key++
5. [ ] Download the VHD
6. [ ] Take note of the ++Project key++ (TODO: create a variable)
7. [ ] Copy and extract the downloaded VHD to the F drive

> ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0090.png)

> ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0091.png)
===

### Install the Appliance
1. [ ] Open the Hyper-V manager
2. [ ] Select ++New++ -> ++Virtual Machine++ (look picture bellow for reference)
3. [ ] Select a name
4. [ ] In Location, specify +++F:\Hyper-V\Virtual Machines\appliance+++
5. [ ] Select +++16384+++ MB of RAM
6. [ ] Connection ++NestedSwitch++
7. [ ] For the hard drive, find the extracted zip file in the F drive, and locate the vhd file
8. [ ] Start the VM
9. [ ] Initially, the VM will have a back scree. Wait for few minutes until it starts

> ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0092.png)

===

### Configure the appliance
1. [ ] Assign a password for the appliance. You can use +++demo!pass123+++
2. [ ] Send a **Ctrl+Alt+Del** command and log in into the VM

	> [+Hint] Do you know how to send Ctrl+Alt+Del to a VM?
  	>
  	> ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0093.png)

3. [ ] Paste the Key we got before. This will take several minutes. TODO: Find the variable
	> [+Hint] If Copy paste does not work
  	>
  	> You can type the clipboard in the VM
    > ![Hyper-V architecture](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0094.png)

4. [ ] Follow the instructions to login to your azure account.
			 Remember that the credentials are in the Resources tab.
6. [ ] In step 2, provide Hyper-V host credentials. 
    > username: +++AdminUSer+++
    > password: +++demo!pass123+++
    > 



===

TODO: How to analyze the data!

How do we plan what to migrate?

Lets pick AirSonic, and I willteach you how to plan the migration

Go to the already created lab
Explore assessment
Look at 6R
- Software
	Tagging example
	Vulnerabilities


Explore business case

===
TODO
Quiz

===


# Excersise 2

GitHub Copilot app modernization is a GitHub Copilot agent that helps upgrade projects to newer versions of .NET and migrate .NET applications to Azure quickly and confidently by guiding you through assessment, solution recommendations, code fixes, and validation - all within Visual Studio.

With this assistant, you can:

- Upgrade to a newer version of .NET.
- Migrate technologies and deploy to Azure.
- Modernize your .NET app, especially when upgrading from .NET Framework.
- Assess your application's code, configuration, and dependencies.
- Plan and set up the right Azure resource.
- Fix issues and apply best practices for cloud migration.
- Validate that your app builds and tests successfully.

===

# Clone the application repo

We have found our first target application to be migrated: `Contoso University`

TODO: It would be better to fork the repo, and be able to commit

1. [] Since we are going to modernize it first, lets get in the shoes of the developers that will execute the migration

  1. [ ] Open Visual Studio
  2. [ ] Select Clone a repository
  3. [ ] In the `Repository Location`, paste this repo ++https://github.com/crgarcia12/migrate-modernize-lab.git++
  4. [ ] Click Clone

1.[] Let's open the solution now. In Visual Studio
  1. [ ] File -> Open
  2. [ ] Navigate to `migrate-modernize-lab`, `src`, `Contoso University`
  3. [ ] Find the file `ContosoUniversity.sln`
  4. [ ] In the `View` menu, click `Solution Explorer`
  5. [ ] Rebuild the solution

      > [!Hint] TODO: Troubles building? Make sure you have Nuget.org package sources


It is not required for the lab, but if you want, you can run the solution in IIS Express (Microsoft Edge)
 
!IMAGE[0030.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0030.png)

Edge will open, and you will see the application running in `https://localhost:44300`


===

# Running a code assessment

We will modernize this application.
The first step is to do a code assessment. For that we will use GitHub Copilot for App Modernization

1. [ ] Right click in the project, and select `Modernize`

!IMAGE[0040.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0040.png)
===


# Upgrade to a newer version

We will do the migration in two steps. First we will upgrade the application to the latest DotNet, since many packages are outdated with known security vulnerabilities,

1. [ ] Right click in the project, and select `Modernize`
2. [ ] Click in `Accept upgrade settings and continue`. Make sure you send the message

!IMAGE[0050.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0050.png)

Let s review copilot proposal

TODO: Point to some details

3. [ ] Review the proposed plan.
4. [ ] Ask what is the most risky part of the upgrade
5. [ ] Ask if there are security vulnerabilities in the current solution
6. [ ] Ask copilot to perform the upgrade
7. [ ] Try to clean and build the solution
8. [ ] If there are erros, tell copilot to fix the errors using the chat
9. [ ] Run the application again, this time as a standalone DotNet application

!IMAGE[0060.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0060.png)

> [!Hint] If you see an error at runtime. Try asking copilot to fix it for you.
>
> For example, you can paste the error message and let Copilot fix it. For example: `SystemInvalidOperation The ConnectionString has not been initialized.` 

TODO: See the lists of commit, if we managed to fork the repo

===

# Modernization part 2: Prepare for cloud

We have upgraded an eight years old application, to the latest version of DorNet.
Lets now find out if we can host it in a modern PaaS service

1. [ ] Right click in the project, and select `Modernize`
2. [ ] This time, we will select Migrate to Azure. Don't forget to send the message!
> !IMAGE[0070.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0070.png)

3. [ ] Copilot made a detailed report for us. Let's take a look at it
       Notice that the report can be exported and shared with other developers in the top right corner
4. Now, let's run the Mandatory Issue: Windows Authenticatio. Click in `Run Task`
> !IMAGE[0080.png](https://raw.githubusercontent.com/crgarcia12/migrate-modernize-lab/refs/heads/main/lab-material/media/0080.png)