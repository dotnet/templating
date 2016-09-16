// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = ['Windows_NT:Release', 'Windows_NT:Debug', 'Ubuntu16.04:x64:Debug', 'OSX:x64:Release']

def static getBuildJobName(def configuration, def os) {
    return configuration.toLowerCase() + '_' + os.toLowerCase()
}


platformList.each { platform ->
    // Calculate names
    def (os, configuration) = platform.tokenize(':')

    // Calculate job name
    def jobName = getBuildJobName(configuration, os)
    def buildCommand = './build.sh -Configuration ${configuration}';

    // Calculate the build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd -Configuration ${configuration}"
    }
    
    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
        // Set the label.
        steps {
            if (os == 'Windows_NT') {
                // Batch
                batchFile(buildCommand)
            }
            else {
                // Shell
                shell(buildCommand)
            }
        }
    }

    Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    Utilities.addXUnitDotNETResults(newJob, '**/*-testResults.xml')
    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${configuration} Build")
}

