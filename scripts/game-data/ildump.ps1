# Minimal IL disassembler using only the .NET runtime's own System.Reflection.Metadata.
# Usage: pwsh ildump.ps1 <assembly> <outfile>
param([string]$asm, [string]$out)
$ErrorActionPreference = 'Stop'
$fs = [System.IO.File]::OpenRead($asm)
$pe = New-Object System.Reflection.PortableExecutable.PEReader($fs)
$md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)

# opcode table from System.Reflection.Emit.OpCodes
$one = @{}; $two = @{}
foreach ($f in [System.Reflection.Emit.OpCodes].GetFields()) {
  $op = $f.GetValue($null)
  $v = [uint16]($op.Value -band 0xFFFF)
  if ($op.Size -eq 1) { $one[[int]$v] = $op } else { $two[[int]($v -band 0xFF)] = $op }
}

function TokName([int]$tok) {
  $h = [System.Reflection.Metadata.Ecma335.MetadataTokens]::EntityHandle($tok)
  try {
    switch ($h.Kind) {
      'MethodDefinition' { $m = $md.GetMethodDefinition([System.Reflection.Metadata.MethodDefinitionHandle]$h); $t = $md.GetTypeDefinition($m.GetDeclaringType()); return "$($md.GetString($t.Name))::$($md.GetString($m.Name))" }
      'FieldDefinition' { $f = $md.GetFieldDefinition([System.Reflection.Metadata.FieldDefinitionHandle]$h); $t = $md.GetTypeDefinition($f.GetDeclaringType()); return "$($md.GetString($t.Name))::$($md.GetString($f.Name))" }
      'MemberReference' { $r = $md.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$h); $p = ''; if ($r.Parent.Kind -eq 'TypeReference') { $p = $md.GetString($md.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$r.Parent).Name) } elseif ($r.Parent.Kind -eq 'TypeDefinition') { $p = $md.GetString($md.GetTypeDefinition([System.Reflection.Metadata.TypeDefinitionHandle]$r.Parent).Name) } elseif ($r.Parent.Kind -eq 'TypeSpecification') { $p = 'generic' }; return "$p::$($md.GetString($r.Name))" }
      'TypeDefinition' { return $md.GetString($md.GetTypeDefinition([System.Reflection.Metadata.TypeDefinitionHandle]$h).Name) }
      'TypeReference' { return $md.GetString($md.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$h).Name) }
      'MethodSpecification' { $ms = $md.GetMethodSpecification([System.Reflection.Metadata.MethodSpecificationHandle]$h); return "spec:" + (TokName ([System.Reflection.Metadata.Ecma335.MetadataTokens]::GetToken($ms.Method))) }
      default { return "$($h.Kind)" }
    }
  } catch { return "tok$tok" }
}

$sb = New-Object System.Text.StringBuilder
foreach ($th in $md.TypeDefinitions) {
  $t = $md.GetTypeDefinition($th)
  $tn = $md.GetString($t.Name)
  [void]$sb.AppendLine("=== TYPE $tn")
  foreach ($fh in $t.GetFields()) {
    $f = $md.GetFieldDefinition($fh)
    $c = ''
    if (-not $f.GetDefaultValue().IsNil) { $cv = $md.GetConstant($f.GetDefaultValue()); $br = $md.GetBlobReader($cv.Value); $c = ' = ' + $br.ReadConstant($cv.TypeCode) }
    [void]$sb.AppendLine("  field $($md.GetString($f.Name))$c")
  }
  foreach ($mh in $t.GetMethods()) {
    $m = $md.GetMethodDefinition($mh)
    [void]$sb.AppendLine("  --- METHOD $tn::$($md.GetString($m.Name))")
    if ($m.RelativeVirtualAddress -eq 0) { continue }
    $body = [System.Reflection.Metadata.PEReaderExtensions]::GetMethodBody($pe, $m.RelativeVirtualAddress)
    $il = $body.GetILBytes()
    $i = 0
    while ($i -lt $il.Length) {
      $b = $il[$i]; $i++
      if ($b -eq 0xFE) { $op = $two[[int]$il[$i]]; $i++ } else { $op = $one[[int]$b] }
      if ($null -eq $op) { [void]$sb.AppendLine("    ??"); break }
      $arg = ''
      switch ($op.OperandType.ToString()) {
        'InlineNone' {}
        'ShortInlineBrTarget' { $arg = [int]$il[$i]; if ($arg -gt 127) { $arg -= 256 }; $i += 1 }
        'ShortInlineI' { $arg = [int]$il[$i]; if ($arg -gt 127) { $arg -= 256 }; $i += 1 }
        'ShortInlineVar' { $arg = $il[$i]; $i += 1 }
        'InlineVar' { $arg = [BitConverter]::ToUInt16($il, $i); $i += 2 }
        'InlineI' { $arg = [BitConverter]::ToInt32($il, $i); $i += 4 }
        'InlineBrTarget' { $arg = [BitConverter]::ToInt32($il, $i); $i += 4 }
        'InlineI8' { $arg = [BitConverter]::ToInt64($il, $i); $i += 8 }
        'ShortInlineR' { $arg = [BitConverter]::ToSingle($il, $i); $i += 4 }
        'InlineR' { $arg = [BitConverter]::ToDouble($il, $i); $i += 8 }
        'InlineString' { $tok = [BitConverter]::ToInt32($il, $i); $i += 4; $arg = '"' + $md.GetUserString([System.Reflection.Metadata.Ecma335.MetadataTokens]::UserStringHandle($tok -band 0xFFFFFF)) + '"' }
        'InlineSwitch' { $n = [BitConverter]::ToInt32($il, $i); $i += 4 + 4 * $n; $arg = "switch($n)" }
        default { $tok = [BitConverter]::ToInt32($il, $i); $i += 4; $arg = TokName $tok }
      }
      [void]$sb.AppendLine("    $($op.Name) $arg")
    }
  }
}
[System.IO.File]::WriteAllText($out, $sb.ToString())
"done: " + (Get-Item $out).Length
