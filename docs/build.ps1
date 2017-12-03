Param(
	[Switch]$Deploy,
	[Switch]$Serve
)
$docfxVersion = "2.28.1"

# Install docfx
& nuget install docfx.console -Version $docfxVersion

if($Deploy){
	# Configuring git credentials
	Write-Host "`n[Configuring git credentials]" -ForegroundColor Green
	& git config --global credential.helper store
	Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:git_access_token):x-oauth-basic@github.com`n"

	& git config --global user.email "$env:git_email"
	& git config --global user.name "$env:git_user"

	# Checkout gh-pages
	Write-Host "`n[Checkout gh-pages]" -ForegroundColor Green
	git clone --quiet --no-checkout --branch=gh-pages https://github.com/NZSmartie/CoAP.Net gh-pages
}

# Build our docs
Write-Host "`n[Build our docs]" -ForegroundColor Green

& .\docfx.console.$docfxVersion\tools\docfx docfx.json (&{If($Serve) {"--serve"}})

if($Deploy){
	git -C gh-pages status
	$pendingChanges = git -C gh-pages status | select-string -pattern "Changes not staged for commit:","Untracked files:" -simplematch
	if ($pendingChanges -ne $null) { 
		# Committing changes
		Write-host "`n[Committing changes]" -ForegroundColor Green
		git -C gh-pages add -A
		git -C gh-pages commit -m "static site regeneration"
		# Pushing changes
		Write-host "`n[Pushing changes]" -ForegroundColor Green
		git -C gh-pages push origin gh-pages --quiet
		Write-Host "`n[Success!]" -ForegroundColor Green
	} 
	else { 
		write-host "`nNo changes to documentation" -ForegroundColor Yellow
	}
}