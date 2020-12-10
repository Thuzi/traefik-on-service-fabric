pipeline {
	agent { label 'dotNet4.x' }
	triggers { issueCommentTrigger('.*test this please.*') }
	post { 
		success {
			deleteDir()
		}
		failure {
			mail to: "cruppert@thuzi.com", bcc: '', body: "Project: ${env.JOB_NAME} <br>Build Number: ${env.BUILD_NUMBER} <br> BUILD URL: ${env.BUILD_URL}", cc: '', charset: 'UTF-8', from: '', mimeType: 'text/html', replyTo: '', subject: "ERROR CI: ${env.JOB_NAME}" ;
		}
	}
	stages {
		stage('Pull-Request Build')
		{
			when {
				anyOf {
					changeRequest target: 'master' 
					triggeredBy 'issueCommentTrigger'
				}
			}
			steps {
				configFileProvider([configFile(fileId: 'NSB_License', variable: 'NSB_License')]) {		
					powershell 'Get-ChildItem -exclude *bin* -filter License.xml -recurse | Where {$_.FullName -notmatch \"bin\"} | foreach{ Write-Host \"   Replacing \" $_.FullName;copy-item $env:NSB_License \"$_.FullName\" }'
				}
				powershell(returnStatus: false, script: '''
					nuget.exe install Cake.LongPath.Module -PreRelease -ExcludeVersion -OutputDirectory "$(get-location)/tools/modules/" -Source https://www.nuget.org/api/v2/
					./build.ps1 -target="ci"
				''')
				//record ci issues to the build.
				/*
                recordIssues (enabledForFailure: true, 
								blameDisabled: true, 
								tool: resharperInspectCode(pattern: 'artifacts/testresults/resharper-inspection.xml'), 
								qualityGates: [
									[threshold: 1, type: 'TOTAL_HIGH', failure: true],
									[threshold: 1, type: 'TOTAL_NORMAL', failure: true],
								])
				// https://github.com/jenkinsci/warnings-ng-plugin/blob/master/src/main/java/io/jenkins/plugins/analysis/core/util/QualityGate.java#L189
                */
				//publishCoverage (adapters: [coberturaAdapter('artifacts/testresults/*/coverage.cobertura.xml')])

				//mstest result reporter
				//step([$class: 'MSTestPublisher', testResultsFile:"**/testresults/*.trx", failOnError: false, keepLongStdio: true])
			}
	}
		stage('Release') {
			when { 
				allOf {
					branch 'master'
					not { triggeredBy 'issueCommentTrigger' }
				}
			}
			steps {
				configFileProvider([configFile(fileId: 'NSB_License', variable: 'NSB_License')]) {		
					powershell 'Get-ChildItem -exclude *bin* -filter License.xml -recurse | Where {$_.FullName -notmatch \"bin\"} | foreach{ Write-Host \"   Replacing \" $_.FullName;copy-item $env:NSB_License \"$_.FullName\" }'
				}
				
				withCredentials([usernamePassword(credentialsId: '3dde0f60-3fa7-451a-a787-9d5b4f64d8e9', usernameVariable: 'GH_UNAME', passwordVariable: 'GH_PASSWORD'),
								string(credentialsId: 'OctopusApiKey', variable:'OCTO_API_KEY'),
								string(credentialsId: 'nuget_api_key', variable:'NUGET_API')]){
					
					powershell returnStatus: false, script: '''
						nuget.exe install Cake.LongPath.Module -PreRelease -ExcludeVersion -OutputDirectory "$(get-location)/tools/modules/" -Source https://www.nuget.org/api/v2/
						./build.ps1 -target=release
					'''
				}
				//mstest result reporter
				//step([$class: 'MSTestPublisher', testResultsFile:"**/testresults/*.trx", failOnError: false, keepLongStdio: true])
				//publishCoverage (adapters: [coberturaAdapter('artifacts/testresults/*/coverage.cobertura.xml')])
			}
		}
	}
}
