$accountName = "Keboo"
$projectSlug = "autodi"
$apiRoot = "https://ci.appveyor.com/api"


#Get the current branch we are working on
$currentBranch = git rev-parse --abbrev-ref HEAD 

#Find the commit hash of the last successful build
$headers = @{
    "Content-type" = "application/json"
}
$branchHistoryUri = "$apiRoot/projects/$accountName/$projectSlug/history?recordsNumber=10&branch=$currentBranch"

"Getting history for branch $currentBranch from $branchHistoryUri"

$branchHistory = Invoke-WebRequest -Uri "$branchHistoryUri" -Headers $headers -Method Get | ConvertFrom-Json

#TODO: Interate on next page of results if we don't have a successful build
$lastSuccess = $branchHistory | Select-Object -expand builds | Select-Object buildid, commitid, status | Where-Object status -eq success | Select-Object -first 1

#TODO: Validate that we have a good SHA commit, with github sometimes there are generated commits that wont be in the tree
"Last Successful build from $currentBranch $lastSuccess"

$currentCommit = git rev-parse --verify HEAD
$changedFiles = git log --pretty=oneline --name-only $lastSuccess.commitid..$currentCommit | Where-Object {$_ -notmatch '^[0-9a-f]{40} ' }

"Changed files between $($lastSuccess.commitid) and $currentCommit"
$changedFiles
