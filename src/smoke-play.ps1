$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$assemblyPath = Join-Path $projectDir 'dist\HuangYueDemo.exe'
$assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($assemblyPath))
$formType = $assembly.GetType('HuangYueDemo.MainForm', $true)
$flags = [System.Reflection.BindingFlags]'Instance, NonPublic'
$form = [System.Activator]::CreateInstance($formType, $true)
$form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
$form.Location = New-Object System.Drawing.Point(-3000, -3000)
$form.ShowInTaskbar = $false
$formType.GetMethod('StartRealtimeDemo', $flags).Invoke($form, $null)
$form.Show()
[System.Windows.Forms.Application]::DoEvents()
$formType.GetMethod('StopRealtimeTimer', $flags).Invoke($form, $null)

$state = $formType.GetField('realtime', $flags).GetValue($form)
$stateType = $state.GetType()
$orderMethod = $formType.GetMethod('RealtimeNodeClicked', $flags)
$selectMethod = $formType.GetMethod('SelectRealtimeUnit', $flags)
$tickMethod = $formType.GetMethod('RealtimeTick', $flags)
$choiceField = $formType.GetField('realtimeChoiceOpen', $flags)
$stage = $formType.GetField('stage', $flags).GetValue($form)
$supportMethod = $formType.GetMethod('CheckPublicSupportEffects', $flags)

function Invoke-Ticks([int]$count) {
    for ($i = 0; $i -lt $count; $i++) {
        $tickMethod.Invoke($form, @($null, [System.EventArgs]::Empty)) | Out-Null
        if ($choiceField.GetValue($form)) { break }
    }
}

function Choose-FirstDecision {
    $overlay = $stage.Controls | Where-Object { $_ -is [System.Windows.Forms.Panel] -and $_.Width -eq 820 } | Select-Object -Last 1
    if ($null -eq $overlay) { throw 'Decision overlay not found.' }
    $button = $overlay.Controls | Where-Object { $_ -is [System.Windows.Forms.Button] } | Select-Object -First 1
    if ($null -eq $button) { throw 'Decision button not found.' }
    $button.PerformClick()
    [System.Windows.Forms.Application]::DoEvents()
}

$granary = $state.Nodes[3]
$ferry = $state.Nodes[5]
$main = $state.Units[0]
$carrier = $state.Units[2]

$state.Support = 10
$supportMethod.Invoke($form, $null) | Out-Null
if (-not $choiceField.GetValue($form)) { throw 'Low-support crisis did not open.' }
Choose-FirstDecision
if ($state.Support -lt 20) { throw 'Low-support choice did not change public support.' }
$state.Support = 38
$state.Food = 4
$state.Morale = 52

$selectMethod.Invoke($form, @($main)) | Out-Null
$orderMethod.Invoke($form, @($granary)) | Out-Null
for ($round = 0; $round -lt 8 -and -not $state.GranaryCaptured; $round++) {
    Invoke-Ticks 800
    if ($choiceField.GetValue($form)) { Choose-FirstDecision }
}
if (-not $state.GranaryCaptured) { throw 'Granary was not captured.' }

$selectMethod.Invoke($form, @($carrier)) | Out-Null
$orderMethod.Invoke($form, @($granary)) | Out-Null
for ($round = 0; $round -lt 8 -and -not $carrier.Loaded; $round++) {
    Invoke-Ticks 400
    if ($choiceField.GetValue($form)) { Choose-FirstDecision }
}
if (-not $carrier.Loaded) { throw 'Carrier did not load grain.' }
$orderMethod.Invoke($form, @($ferry)) | Out-Null
for ($round = 0; $round -lt 8 -and -not $state.Finished; $round++) {
    Invoke-Ticks 800
    if ($choiceField.GetValue($form)) { Choose-FirstDecision }
}

if (-not $state.Finished) { throw 'Realtime scenario did not reach an ending.' }
Write-Output ("Finished={0}; CarrierLoaded={1}; Food={2}; Morale={3}; Support={4}; Remaining={5}" -f `
    $state.Finished, $carrier.Loaded, $state.Food, $state.Morale, $state.Support, $state.PlayerStrength())
$form.Hide()
$form.Dispose()
