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
$startMethod = $formType.GetMethod('StartRealtimeDemo', $flags)
$stopMethod = $formType.GetMethod('StopRealtimeTimer', $flags)
$orderMethod = $formType.GetMethod('RealtimeNodeClicked', $flags)
$selectMethod = $formType.GetMethod('SelectRealtimeUnit', $flags)
$tickMethod = $formType.GetMethod('RealtimeTick', $flags)
$blockMethod = $formType.GetMethod('SetBlockingOrder', $flags)
$detectMethod = $formType.GetMethod('DetectRealtimeEngagements', $flags)

function Start-TestBattle {
    $startMethod.Invoke($form, $null) | Out-Null
    $form.Show()
    [System.Windows.Forms.Application]::DoEvents()
    $stopMethod.Invoke($form, $null) | Out-Null
    return $formType.GetField('realtime', $flags).GetValue($form)
}

$state = Start-TestBattle
$main = $state.Units[0]
$granary = $state.Nodes[3]
$selectMethod.Invoke($form, @($main)) | Out-Null
$orderMethod.Invoke($form, @($granary)) | Out-Null
if ($main.Route.Count -lt 2) { throw 'Road route did not contain intermediate waypoints.' }
$tickMethod.Invoke($form, @($null, [System.EventArgs]::Empty)) | Out-Null
if ($main.CurrentSpeed -le 0 -or $main.CurrentSpeed -gt $main.Speed) {
    throw 'Smooth acceleration was not applied.'
}
$routeWaypoints = $main.Route.Count
$smoothSpeed = $main.CurrentSpeed

$state = Start-TestBattle
$main = $state.Units[0]
$vanguard = $state.Units[4]
$main.X = 0.40
$main.Y = 0.40
$vanguard.X = 0.466
$vanguard.Y = 0.40
$vanguard.Active = $true
$beforeStrength = $vanguard.Strength
$beforeX = $vanguard.X
$blockMethod.Invoke($form, @($main)) | Out-Null
$tickMethod.Invoke($form, @($null, [System.EventArgs]::Empty)) | Out-Null
if ($main.AmbushReady) { throw 'Ambush was not consumed on interception.' }
if (-not $main.InCombat -or -not $vanguard.InCombat) { throw 'Blocking did not intercept the enemy.' }
if ($vanguard.Strength -ge $beforeStrength) { throw 'Blocking ambush caused no enemy loss.' }
if ([Math]::Abs($vanguard.X - $beforeX) -gt 0.0001) { throw 'Intercepted enemy moved through the blocking line.' }
$ambushLoss = [Math]::Round($beforeStrength - $vanguard.Strength, 2)

$state = Start-TestBattle
$main = $state.Units[0]
$scout = $state.Units[1]
$guard = $state.Units[3]
$manor = $state.Nodes[2]
$village = $state.Nodes[1]
$mainLength = [Math]::Sqrt([Math]::Pow($manor.X - $guard.X, 2) + [Math]::Pow($manor.Y - $guard.Y, 2))
$scoutLength = [Math]::Sqrt([Math]::Pow($village.X - $guard.X, 2) + [Math]::Pow($village.Y - $guard.Y, 2))
$main.X = $guard.X + [float](($manor.X - $guard.X) / $mainLength * 0.040)
$main.Y = $guard.Y + [float](($manor.Y - $guard.Y) / $mainLength * 0.040)
$scout.X = $guard.X + [float](($village.X - $guard.X) / $scoutLength * 0.040)
$scout.Y = $guard.Y + [float](($village.Y - $guard.Y) / $scoutLength * 0.040)
$detectMethod.Invoke($form, $null) | Out-Null
if (-not $guard.PincerActive -or -not $main.PincerActive -or -not $scout.PincerActive) {
    throw 'Two-direction pincer was not detected.'
}

Write-Output ("RouteWaypoints={0}; SmoothSpeed={1:N4}; BlockingLoss={2}; Pincer={3}" -f `
    $routeWaypoints, $smoothSpeed, $ambushLoss, $guard.PincerActive)
$form.Hide()
$form.Dispose()
