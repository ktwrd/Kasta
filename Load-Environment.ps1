Get-Content .env | foreach {
	$idxC = $_.IndexOf('#')
	$str = $_;
	if ($idxC -ne -1) {
		$str = $_.Substring(0, $idxC);
	}
	$idx = $str.IndexOf('=')
	if ($idx -eq -1) {
		continue
	}
	$name = $str.Substring(0, $idx)
	$value = $str.Substring($idx + 1).Trim('"');
	if ([string]::IsNullOrWhiteSpace($name) || $name.Contains('#')) {
		continue
	}
	Set-Content env:\$name $value
}