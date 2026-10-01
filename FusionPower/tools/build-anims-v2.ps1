param([string]$Kanimal = 'kanimal-cli.exe', [string]$Only)

$ErrorActionPreference = 'Stop'
$env:DOTNET_ROLL_FORWARD = 'Major'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'art/source'
$assets = Join-Path $root 'anim/assets'
$culture = [System.Globalization.CultureInfo]::InvariantCulture

$specs = @(
    @{ Name='baiye_isotope_separator'; Image='separator.png'; Empty='separator-housing.png'; W=1254; H=1254; Scale=.24; Pivot=.06; MeterX=83; MeterY=102; MeterW=13; MeterH=72; Left=@(64,210,245); Right=@(75,149,245); Motion='rotor'; MotionY=135; MotionScale=.05; MotionW=92; MotionH=92; FxW=70; FxH=92 },
    @{ Name='baiye_tritium_breeder'; Image='breeder.png'; Empty='breeder-housing.png'; W=1254; H=1254; Scale=.24; Pivot=.06; MeterX=61; MeterY=94; MeterW=3; MeterH=60; Left=@(220,185,74); Right=@(186,105,245); Motion='piston'; MotionY=128; MotionScale=.051; MotionW=58; MotionH=116; FxW=48; FxH=78 },
    @{ Name='baiye_fusion_reactor'; Image='reactor.png'; Empty='reactor-empty.png'; W=1402; H=1122; Scale=.35; Pivot=.08; MeterX=201; MeterY=98; MeterW=15; MeterH=67; Left=@(54,202,253); Right=@(189,106,250); Motion='ring'; MotionY=169; MotionScale=.084; MotionW=152; MotionH=152; FxW=73; FxH=73 },
    @{ Name='baiye_triple_alpha'; Image='triple-alpha-housing.png'; Empty='triple-alpha-housing.png'; W=1254; H=1254; Scale=.32; Pivot=.1061; MeterX=120; LeftX=-122; RightX=117; MeterY=108; MeterW=17; MeterH=103; Left=@(73,201,235); Right=@(156,164,177); Motion='ring'; MotionY=154; MotionScale=.14; MotionW=1254; MotionH=1254; FxW=80; FxH=80; IconMotion=$true }
)
if($Only) { $specs=@($specs | Where-Object {$_.Name -eq $Only}); if(!$specs.Count){throw "Unknown animation: $Only"} }

function F([double]$n) { return $n.ToString('0.####', $culture) }

function Make-Icon([string]$inputPath, [string]$outputPath, [hashtable]$spec, [string]$work) {
    $sourceImage = [System.Drawing.Image]::FromFile($inputPath)
    if($spec.IconMotion) {
        $composite=[System.Drawing.Bitmap]::new($sourceImage)
        $cg=[System.Drawing.Graphics]::FromImage($composite)
        $part=[System.Drawing.Image]::FromFile((Join-Path $work 'motion_0.png'))
        $pw=$part.Width*$spec.MotionScale/$spec.Scale; $ph=$part.Height*$spec.MotionScale/$spec.Scale
        $cg.DrawImage($part,[System.Drawing.RectangleF]::new([float](($sourceImage.Width-$pw)/2),[float]($sourceImage.Height*(1-$spec.Pivot)-$spec.MotionY/$spec.Scale-$ph/2),[float]$pw,[float]$ph))
        $part.Dispose(); $cg.Dispose(); $sourceImage.Dispose(); $sourceImage=$composite
    }
    $image = [System.Drawing.Bitmap]::new(128,128,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($image)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $fit = [math]::Min(116.0/$sourceImage.Width,116.0/$sourceImage.Height)
    $w = [int][math]::Round($sourceImage.Width*$fit)
    $h = [int][math]::Round($sourceImage.Height*$fit)
    $g.DrawImage($sourceImage,[System.Drawing.Rectangle]::new([int]((128-$w)/2),[int]((128-$h)/2),$w,$h))
    $image.Save($outputPath,[System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $image.Dispose(); $sourceImage.Dispose()
}

function Make-Motion([hashtable]$spec,[string]$outputPath) {
    $w=[int]$spec.MotionW; $h=[int]$spec.MotionH
    $image=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($image)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $silver=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(245,202,220,230),6)
    $dark=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(238,31,38,49))
    $copper=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(245,225,156,94),10)
    $cyan=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(240,89,208,251),7)
    if($spec.Motion -eq 'rotor') {
        $g.FillEllipse($dark,8,8,$w-16,$h-16)
        $g.DrawEllipse($silver,8,8,$w-16,$h-16)
        for($i=0;$i -lt 4;$i++) {
            $a=$i*[math]::PI/2
            $g.DrawLine($cyan,[float]($w/2+[math]::Cos($a)*13),[float]($h/2+[math]::Sin($a)*13),[float]($w/2+[math]::Cos($a)*35),[float]($h/2+[math]::Sin($a)*35))
        }
        $g.FillEllipse($dark,[float]($w/2-10),[float]($h/2-10),20,20)
    } elseif($spec.Motion -eq 'piston') {
        $purple=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235,176,109,243))
        $metal=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(250,178,188,200))
        $g.FillRectangle($dark,17,3,$w-34,$h-6)
        $g.FillRectangle($purple,21,14,$w-42,$h-28)
        $g.FillRectangle($metal,5,10,$w-10,19)
        $g.FillRectangle($metal,2,$h-34,$w-4,24)
        $g.DrawLine($silver,23,19,23,$h-18)
        $purple.Dispose(); $metal.Dispose()
    } else {
        $g.DrawEllipse($copper,11,11,$w-22,$h-22)
        $g.DrawEllipse($silver,21,21,$w-42,$h-42)
        $g.DrawArc($cyan,29,29,$w-58,$h-58,8,145)
        $g.DrawArc($cyan,29,29,$w-58,$h-58,188,145)
        for($i=0;$i -lt 4;$i++) {
            $a=$i*[math]::PI/2
            $g.DrawLine($silver,[float]($w/2+[math]::Cos($a)*49),[float]($h/2+[math]::Sin($a)*49),[float]($w/2+[math]::Cos($a)*62),[float]($h/2+[math]::Sin($a)*62))
        }
    }
    $image.Save($outputPath,[System.Drawing.Imaging.ImageFormat]::Png)
    $silver.Dispose(); $copper.Dispose(); $cyan.Dispose(); $dark.Dispose(); $g.Dispose(); $image.Dispose()
}

function Make-Meter([string]$work,[string]$side,[int]$w,[int]$h,[int[]]$rgb,[string]$shape) {
    for($step=0;$step -le 50;$step++) {
        $image=[System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $fill=[int][math]::Round($h*$step/50.0)
        for($y=0;$y -lt $fill;$y++) {
            for($x=0;$x -lt $w;$x++) {
                $row=$h-1-$y
                # The breeder has two viewing slots separated by a metal band.
                # Leave that band visible rather than painting over its stripes.
                if($shape -eq 'piston' -and $row -gt 16 -and $row -lt 44){continue}
                $radius=$w/2.0
                $capY=if($row -lt $radius){$radius-$row-.5}elseif($row -ge $h-$radius){$row-($h-$radius)+.5}else{0}
                if($capY -gt 0 -and [math]::Pow($x+.5-$radius,2)+$capY*$capY -gt $radius*$radius){continue}
                $shine=1.08-0.38*[math]::Abs(($x+0.5)/$w-0.5)*2
                $image.SetPixel($x,$h-1-$y,[System.Drawing.Color]::FromArgb(228,
                    [int][math]::Min(255,$rgb[0]*$shine),[int][math]::Min(255,$rgb[1]*$shine),[int][math]::Min(255,$rgb[2]*$shine)))
            }
        }
        $image.Save((Join-Path $work "meter_$($side)_$step.png"),[System.Drawing.Imaging.ImageFormat]::Png)
        $image.Dispose()
    }
}

function Make-Flux([hashtable]$spec,[string]$outputPath) {
    $image=[System.Drawing.Bitmap]::new($spec.FxW,$spec.FxH,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($image)
    $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $rgb=if($spec.Motion -eq 'piston'){$spec.Right}else{$spec.Left}
    $brush=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(70,$rgb[0],$rgb[1],$rgb[2]))
    $pen=[System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(185,$rgb[0],$rgb[1],$rgb[2]),2)
    $g.FillEllipse($brush,6,6,$spec.FxW-12,$spec.FxH-12)
    if($spec.Motion -eq 'ring') {
        $g.DrawArc($pen,9,9,$spec.FxW-18,$spec.FxH-18,20,145)
        $g.DrawArc($pen,15,15,$spec.FxW-30,$spec.FxH-30,190,145)
    } else {
        for($i=0;$i -lt 3;$i++){$y=12+$i*($spec.FxH-24)/3;$g.DrawArc($pen,8,[int]$y,$spec.FxW-16,16,12,280)}
    }
    $image.Save($outputPath,[System.Drawing.Imaging.ImageFormat]::Png)
    $pen.Dispose();$brush.Dispose();$g.Dispose();$image.Dispose()
}

function Add-Line([string]$line) { $script:xml.Add($line) }
function Meter-X([hashtable]$spec,[string]$side) {
    if($side -eq 'left') {if($spec.ContainsKey('LeftX')){return $spec.LeftX};return -$spec.MeterX}
    if($spec.ContainsKey('RightX')){return $spec.RightX};return $spec.MeterX
}

function Add-Still([int]$id,[string]$name,[int]$file,[string]$symbol,[double]$scale) {
    $size=F $scale
    Add-Line "    <animation id=`"$id`" name=`"$name`" length=`"1000`" looping=`"true`">"
    if($symbol -like 'body*') {
        Add-Line '      <mainline><key id="0"><object_ref id="0" timeline="0" key="0" z_index="0"/><object_ref id="1" timeline="1" key="0" z_index="3"/><object_ref id="2" timeline="2" key="0" z_index="4"/><object_ref id="3" timeline="3" key="0" z_index="2"/></key></mainline>'
    } else {
        Add-Line '      <mainline><key id="0"><object_ref id="0" timeline="0" key="0" z_index="0"/></key></mainline>'
    }
    Add-Line "      <timeline id=`"0`" name=`"$symbol`"><key id=`"0`"><object folder=`"0`" file=`"$file`" x=`"0`" y=`"0`" scale_x=`"$size`" scale_y=`"$size`"/></key></timeline>"
    if($symbol -like 'body*') {
        Add-Line "      <timeline id=`"1`" name=`"meter_left_target_0`"><key id=`"0`"><object folder=`"0`" file=`"4`" x=`"$(Meter-X $script:currentSpec 'left')`" y=`"$($script:currentSpec.MeterY)`"/></key></timeline>"
        Add-Line "      <timeline id=`"2`" name=`"meter_right_target_0`"><key id=`"0`"><object folder=`"0`" file=`"5`" x=`"$(Meter-X $script:currentSpec 'right')`" y=`"$($script:currentSpec.MeterY)`"/></key></timeline>"
        Add-Line "      <timeline id=`"3`" name=`"motion_0`"><key id=`"0`"><object folder=`"0`" file=`"3`" x=`"0`" y=`"$($script:currentSpec.MotionY)`" scale_x=`"$(F $script:currentSpec.MotionScale)`" scale_y=`"$(F $script:currentSpec.MotionScale)`"/></key></timeline>"
    }
    Add-Line '    </animation>'
}

function Add-Working([int]$id,[string]$name,[hashtable]$spec,[int]$frames) {
    $duration=$frames*100
    $looping=if($name -eq 'working_loop'){'true'}else{'false'}
    Add-Line "    <animation id=`"$id`" name=`"$name`" length=`"$duration`" interval=`"100`" looping=`"$looping`">"
    Add-Line '      <mainline>'
    for($i=0;$i -lt $frames;$i++) {
        $t=$i*100
        $refs=for($j=0;$j -le 4;$j++){$z=@(0,2,3,4,1)[$j];"<object_ref id=`"$j`" timeline=`"$j`" key=`"$i`" z_index=`"$z`"/>"}
        Add-Line "        <key id=`"$i`" time=`"$t`">$($refs -join '')</key>"
    }
    Add-Line '      </mainline>'
    for($j=0;$j -le 4;$j++) {
        $symbol=@('body_0','motion_0','meter_left_target_0','meter_right_target_0','flux_0')[$j]
        $fileId=@(0,3,4,5,1)[$j]
        Add-Line "      <timeline id=`"$j`" name=`"$symbol`">"
        for($i=0;$i -lt $frames;$i++) {
            $t=$i*100; $cycle=$i*2*[math]::PI/$frames
            $p=if($frames -gt 1){$i/($frames-1.0)}else{0}
            $strength=if($name -eq 'working_pre'){$p}elseif($name -eq 'working_pst'){1-$p}else{1}
            $bodyScale=F ($spec.Scale*(1+.004*$strength*[math]::Sin($cycle)))
            $bx=F (1.5*$strength*[math]::Sin($cycle))
            $by=F (1.5*$strength*[math]::Cos($cycle))
            $alpha=F (.12+.67*$strength*(.88+.12*[math]::Sin($cycle)))
            $travel=if($spec.Motion -eq 'piston'){8}elseif($spec.Motion -eq 'rotor'){4}else{0}
            $my=F ($spec.MotionY+$travel*$strength*[math]::Sin($cycle))
            $angle=if($spec.Motion -eq 'ring'){F ($i*360.0/$frames*$strength)}else{0}
            $motionScale=F $spec.MotionScale
            $motionScaleX=if($spec.Motion -eq 'rotor'){F ($spec.MotionScale*(.95+.05*[math]::Cos($cycle)))}else{$motionScale}
            if($j -eq 0){$attrs="x=`"$bx`" y=`"$by`" scale_x=`"$bodyScale`" scale_y=`"$bodyScale`""}
            elseif($j -eq 1){$attrs="x=`"$bx`" y=`"$my`" angle=`"$angle`" scale_x=`"$motionScaleX`" scale_y=`"$motionScale`""}
            elseif($j -eq 4){$fxAngle=if($spec.Motion -eq 'ring'){$angle}else{0};$attrs="x=`"$bx`" y=`"$($spec.MotionY)`" angle=`"$fxAngle`" a=`"$(F $strength)`""}
            else {
                $x=if($j -eq 2){Meter-X $spec 'left'}else{Meter-X $spec 'right'}
                $attrs="x=`"$(F ($x+[double]$bx))`" y=`"$(F ($spec.MeterY+[double]$by))`""
            }
            Add-Line "        <key id=`"$i`" time=`"$t`"><object folder=`"0`" file=`"$fileId`" $attrs/></key>"
        }
        Add-Line '      </timeline>'
    }
    Add-Line '    </animation>'
}

function Add-MeterAnim([int]$id,[string]$side,[int]$firstFile) {
    Add-Line "    <animation id=`"$id`" name=`"meter_$side`" length=`"5100`" interval=`"100`" looping=`"false`">"
    Add-Line '      <mainline>'
    for($i=0;$i -le 50;$i++){$t=$i*100;Add-Line "        <key id=`"$i`" time=`"$t`"><object_ref id=`"0`" timeline=`"0`" key=`"$i`" z_index=`"0`"/></key>"}
    Add-Line '      </mainline>'
    Add-Line "      <timeline id=`"0`" name=`"meter_$($side)_0`">"
    for($i=0;$i -le 50;$i++){$t=$i*100;$file=$firstFile+$i;Add-Line "        <key id=`"$i`" time=`"$t`"><object folder=`"0`" file=`"$file`" x=`"0`" y=`"0`"/></key>"}
    Add-Line '      </timeline>'
    Add-Line '    </animation>'
}

foreach($spec in $specs) {
    $script:currentSpec=$spec
    $work=Join-Path $source $spec.Name
    $dest=Join-Path $assets $spec.Name
    New-Item -ItemType Directory -Force $work,$dest | Out-Null
    Copy-Item (Join-Path $source $spec.Empty) (Join-Path $work 'body_0.png') -Force
    $obsoleteBody=Join-Path $work 'body_1.png'
    if(Test-Path -LiteralPath $obsoleteBody){Remove-Item -LiteralPath $obsoleteBody}
    $motionArt=Join-Path (Join-Path $source 'motion-parts') "$($spec.Name).png"
    if(Test-Path -LiteralPath $motionArt) {
        $motionImage=[System.Drawing.Image]::FromFile($motionArt)
        $shrink=[math]::Min(1.0,512.0/[math]::Max($motionImage.Width,$motionImage.Height))
        $spec.MotionW=[int][math]::Round($motionImage.Width*$shrink)
        $spec.MotionH=[int][math]::Round($motionImage.Height*$shrink)
        $spec.MotionScale/=$shrink
        $packed=[System.Drawing.Bitmap]::new($spec.MotionW,$spec.MotionH,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g=[System.Drawing.Graphics]::FromImage($packed)
        $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.DrawImage($motionImage,0,0,$spec.MotionW,$spec.MotionH)
        $packed.Save((Join-Path $work 'motion_0.png'),[System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose();$packed.Dispose()
        $motionImage.Dispose()
    } else {
        Make-Motion $spec (Join-Path $work 'motion_0.png')
        $spec.MotionScale=1
    }
    Make-Icon (Join-Path $source $spec.Image) (Join-Path $work 'ui_0.png') $spec $work
    Make-Meter $work 'left' $spec.MeterW $spec.MeterH $spec.Left $spec.Motion
    Make-Meter $work 'right' $spec.MeterW $spec.MeterH $spec.Right $spec.Motion
    Make-Flux $spec (Join-Path $work 'flux_0.png')
    foreach($side in @('left','right')) {
        $target=[System.Drawing.Bitmap]::new(4,4,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $target.SetPixel(1,1,[System.Drawing.Color]::FromArgb(1,255,255,255))
        $target.Save((Join-Path $work "meter_$($side)_target_0.png"),[System.Drawing.Imaging.ImageFormat]::Png)
        $target.Dispose()
    }

    $script:xml=[System.Collections.Generic.List[string]]::new()
    Add-Line '<?xml version="1.0" encoding="utf-8"?>'
    Add-Line '<spriter_data scml_version="1.0" generator="BaiyeFusionPower" generator_version="2.0">'
    Add-Line '  <folder id="0">'
    for($i=0;$i -le 107;$i++) {
        if($i -eq 0){$file='body_0.png';$w=$spec.W;$h=$spec.H;$pivot=F $spec.Pivot}
        elseif($i -eq 1){$file='flux_0.png';$w=$spec.FxW;$h=$spec.FxH;$pivot='0.5'}
        elseif($i -eq 2){$file='ui_0.png';$w=128;$h=128;$pivot='0.5'}
        elseif($i -eq 3){$file='motion_0.png';$w=$spec.MotionW;$h=$spec.MotionH;$pivot='0.5'}
        elseif($i -le 5){$side=if($i -eq 4){'left'}else{'right'};$file="meter_$($side)_target_0.png";$w=4;$h=4;$pivot='0.5'}
        else {$side=if($i -le 56){'left'}else{'right'};$step=if($i -le 56){$i-6}else{$i-57};$file="meter_$($side)_$step.png";$w=$spec.MeterW;$h=$spec.MeterH;$pivot='0'}
        Add-Line "    <file id=`"$i`" name=`"$file`" width=`"$w`" height=`"$h`" pivot_x=`"0.5`" pivot_y=`"$pivot`"/>"
    }
    Add-Line '  </folder>'
    Add-Line "  <entity id=`"0`" name=`"$($spec.Name)`">"
    Add-Still 0 'off' 0 'body_0' $spec.Scale
    Add-Still 1 'on' 0 'body_0' $spec.Scale
    Add-Working 2 'working_pre' $spec 8
    Add-Working 3 'working_loop' $spec 16
    Add-Working 4 'working_pst' $spec 8
    Add-Still 5 'idle' 0 'body_0' $spec.Scale
    Add-Still 6 'place' 0 'body_0' $spec.Scale
    Add-Still 7 'ui' 2 'ui_0' 1
    Add-MeterAnim 8 'left' 6
    Add-MeterAnim 9 'right' 57
    Add-Line '  </entity>'
    Add-Line '</spriter_data>'
    [System.IO.File]::WriteAllLines((Join-Path $work "$($spec.Name).scml"),$script:xml,[System.Text.UTF8Encoding]::new($false))
    & $Kanimal kanim (Join-Path $work "$($spec.Name).scml") -o $dest
    if($LASTEXITCODE -ne 0){throw "KAnim conversion failed: $($spec.Name)"}
}
