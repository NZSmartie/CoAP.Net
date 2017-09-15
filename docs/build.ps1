$docfxVersion = "2.24.0"
$VisualStudioVersion = "15.0";
$DotnetSDKVersion = "2.0.0";

# Get dotnet paths
$MSBuildExtensionsPath = "C:\Program Files\dotnet\sdk\" + $DotnetSDKVersion;
$MSBuildSDKsPath = $MSBuildExtensionsPath + "\SDKs";

# Get Visual Studio install path
$VSINSTALLDIR =  $(Get-ItemProperty "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7").$VisualStudioVersion;

# Add Visual Studio environment variables
$env:VisualStudioVersion = $VisualStudioVersion;
$env:VSINSTALLDIR = $VSINSTALLDIR;

# Add dotnet environment variables
$env:MSBuildExtensionsPath = $MSBuildExtensionsPath;
$env:MSBuildSDKsPath = $MSBuildSDKsPath;

# Install docfx
& nuget install docfx.console -Version $docfxVersion

# Checkout gh-pages
Write-Host "- Set config settings...."

& git config --global credential.helper store
Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:git_access_token):x-oauth-basic@github.com`n"

& git config --global user.email "$env:git_email"
& git config --global user.name "$env:git_user"

Write-Host "- Clone gh-pages branch...."
git clone --quiet --no-checkout --branch=gh-pages https://github.com/NZSmartie/CoAP.Net gh-pages

Write-Host "- Generate the site contents..."
# Build our docs
& .\docfx.console.$docfxVersion\tools\docfx docfx.json

git -C gh-pages status
$thereAreChanges = git -C gh-pages status | select-string -pattern "Changes not staged for commit:","Untracked files:" -simplematch
if ($thereAreChanges -ne $null) { 
    Write-host "- Committing changes to documentation..."
    git -C gh-pages add --all
    git -C gh-pages status
    git -C gh-pages commit -m "static site regeneration"
    git -C gh-pages status
    Write-Host "- Push it...."
    git -C gh-pages push origin gh-pages --quiet
    Write-Host "- Pushed it good!"
} 
else { 
    write-host "- No changes to documentation to commit"
}