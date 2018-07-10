// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = ['Linux:x64:Release', 'Debian8.2:x64:Debug', 'Ubuntu:x64:Release', 'Ubuntu16.04:x64:Debug', 'Ubuntu16.10:x64:Debug', 'Windows_NT:x64:Release', 'Windows_NT:x86:Debug', 'RHEL7.2:x64:Release', 'CentOS7.1:x64:Debug']
//Temporarily removing OSX10.12 from the list of configuratons to build until we can find a way to troubleshoot it
//  'OSX10.12:x64:Release', 

def static getBuildJobName(def configuration, def os, def architecture) {
    return configuration.toLowerCase() + '_' + os.toLowerCase() + '_' + architecture.toLowerCase()
}


platformList.each { platform ->
    // Calculate names
    def (os, architecture, configuration) = platform.tokenize(':')
    def osUsedForMachineAffinity = os;

    // Calculate job name
    def jobName = getBuildJobName(configuration, os, architecture)
    def buildCommand = '';

    // Calculate the build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd -Configuration ${configuration}"
    }
    else if (os == 'Windows_2016') {
        buildCommand = ".\\build.cmd -Configuration ${configuration}"
    }
    else if (os == 'Ubuntu') {
        buildCommand = "./build.sh --configuration ${configuration}"
    }
    else if (os == 'Linux') {
        osUsedForMachineAffinity = 'Ubuntu16.04';
        buildCommand = "./build.sh --configuration ${configuration}"
    }
    else {
        // Jenkins non-Ubuntu CI machines don't have docker
        buildCommand = "./build.sh --configuration ${configuration}"
    }

    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
        // Set the label.
        steps {
            if (os == 'Windows_NT' || os == 'Windows_2016') {
                // Batch
                batchFile(buildCommand)
            }
            else {
                // Shell
                shell(buildCommand)
            }
        }
    }

    Utilities.setMachineAffinity(newJob, osUsedForMachineAffinity, 'latest-or-auto')
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    Utilities.addMSTestResults(newJob, '**/*.trx')
    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${architecture} ${configuration} Build")
}

