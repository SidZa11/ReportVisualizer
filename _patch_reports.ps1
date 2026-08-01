$ErrorActionPreference = "Stop"
$reportsDir = Join-Path $PSScriptRoot "ReportViewer\Reports"
if (-not (Test-Path $reportsDir)) {
  Write-Host "Reports directory not found: $reportsDir"
  exit 1
}
$files = Get-ChildItem -Path $reportsDir -Filter *.rdl
foreach ($file in $files) {
  $content = Get-Content -Raw -LiteralPath $file.FullName -Encoding UTF8
  $original = $content

  # Ensure every Tablix element has RepeatColumnHeaders=true and RepeatRowHeaders=true right before <DataSetName>
  # Match Tablix blocks
  $content = [regex]::Replace($content, '(?s)(<Tablix\b[^>]*>.*?)(?<before><DataSetName>)', {
    param($m)
    $tablix = $m.Groups[1].Value
    $before = $m.Groups['before'].Value
    if ($tablix -notmatch '<RepeatColumnHeaders>') {
      $tablix = $tablix + "`r`n            <RepeatColumnHeaders>true</RepeatColumnHeaders>"
    }
    if ($tablix -notmatch '<RepeatRowHeaders>') {
      $tablix = $tablix + "`r`n            <RepeatRowHeaders>true</RepeatRowHeaders>"
    }
    return $tablix + $before
  }, [System.Text.RegularExpressions.RegexOptions]::Singleline)

  # Process each TablixMember inside TablixRowHierarchy/TablixMembers that has no <Group> element (static headers)
  # Ensure they have KeepWithGroup=After and RepeatOnNewPage=true
  $content = [regex]::Replace($content, '(?s)(<TablixRowHierarchy>\s*<TablixMembers>)(.*?)(</TablixMembers>\s*</TablixRowHierarchy>)', {
    param($m)
    $prefix = $m.Groups[1].Value
    $members = $m.Groups[2].Value
    $suffix = $m.Groups[3].Value
    # Replace each static TablixMember (no child <Group>)
    $members = [regex]::Replace($members, '(?s)<TablixMember>\s*(?!\s*<Group>)(.*?)\s*</TablixMember>', {
      param($m2)
      $inner = $m2.Groups[1].Value
      if ($inner -match '<Group\b') { return $m2.Value }
      if ($inner -notmatch '<KeepWithGroup>') {
        $inner = "`r`n                  <KeepWithGroup>After</KeepWithGroup>$inner"
      }
      if ($inner -notmatch '<RepeatOnNewPage>') {
        $inner = "`r`n                  <RepeatOnNewPage>true</RepeatOnNewPage>$inner"
      }
      return "<TablixMember>$inner</TablixMember>"
    }, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    return $prefix + $members + $suffix
  }, [System.Text.RegularExpressions.RegexOptions]::Singleline)

  if ($content -ne $original) {
    Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8 -NoNewline
    Write-Host "Updated: $($file.Name)"
  } else {
    Write-Host "No changes: $($file.Name)"
  }
}
