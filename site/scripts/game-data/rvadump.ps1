# Print the static array initialiser blobs (<PrivateImplementationDetails> fields with an RVA).
param([string]$asm)
$all = [System.IO.File]::ReadAllBytes($asm)
$fs = [System.IO.File]::OpenRead($asm)
$pe = New-Object System.Reflection.PortableExecutable.PEReader($fs)
$md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
foreach ($fh in $md.FieldDefinitions) {
  $f = $md.GetFieldDefinition($fh)
  $rva = $f.GetRelativeVirtualAddress()
  if ($rva -eq 0) { continue }
  $off = -1
  foreach ($s in $pe.PEHeaders.SectionHeaders) { if ($rva -ge $s.VirtualAddress -and $rva -lt $s.VirtualAddress + $s.VirtualSize) { $off = $rva - $s.VirtualAddress + $s.PointerToRawData } }
  $name = $md.GetString($f.Name)
  $ints = for ($i = 0; $i -lt 24; $i += 4) { [BitConverter]::ToInt32($all, $off + $i) }
  $floats = for ($i = 0; $i -lt 16; $i += 4) { [BitConverter]::ToSingle($all, $off + $i) }
  "{0}  ints: {1}   floats: {2}" -f $name.Substring(0, 8), ($ints -join ','), ($floats -join ',')
}
