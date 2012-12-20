using System;
using System.Runtime.InteropServices;
using System.Text;
using NewLife.Security;

namespace NewLife.Reflection
{
    /// <summary>X86内联汇编</summary>
    public class AsmX86
    {
        #region 原生WinApi
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        static extern Int32 CloseHandle(Int32 hObject);

        [DllImport("kernel32.dll")]
        static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] Byte[] buffer, Int32 size, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern Int32 WriteProcessMemory(Int32 hProcess, Int32 lpBaseAddress, Byte[] buffer, Int32 size, Int32 lpNumberOfBytesWritten);

        [DllImport("kernel32", EntryPoint = "CreateRemoteThread")]
        static extern Int32 CreateRemoteThread(Int32 hProcess, Int32 lpThreadAttributes, Int32 dwStackSize, Int32 lpStartAddress, Int32 lpParameter, Int32 dwCreationFlags, ref Int32 lpThreadId);

        [DllImport("Kernel32.dll")]
        static extern System.Int32 VirtualAllocEx(IntPtr hProcess, Int32 lpAddress, Int32 dwSize, Int16 flAllocationType, Int16 flProtect);

        [DllImport("Kernel32.dll")]
        static extern System.Int32 VirtualAllocEx(Int32 hProcess, Int32 lpAddress, Int32 dwSize, Int32 flAllocationType, Int32 flProtect);

        [DllImport("Kernel32.dll")]
        static extern System.Int32 VirtualFreeEx(Int32 hProcess, Int32 lpAddress, Int32 dwSize, Int32 flAllocationType);

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
        static extern Int32 OpenProcess(Int32 dwDesiredAccess, Int32 bInheritHandle, Int32 dwProcessId);
        #endregion

        #region 常量
        private const Int32 PAGE_EXECUTE_READWRITE = 0x4;
        private const Int32 MEM_COMMIT = 4096;
        private const Int32 MEM_RELEASE = 0x8000;
        private const Int32 MEM_DECOMMIT = 0x4000;
        private const Int32 PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const Int32 PROCESS_CREATE_THREAD = 0x2;
        private const Int32 PROCESS_VM_OPERATION = 0x8;
        private const Int32 PROCESS_VM_WRITE = 0x20;
        #endregion

        /// <summary>汇编代码</summary>
        StringBuilder Builder = new StringBuilder();

        String intTohex(Int32 value, Int32 num)
        {
            String str1;
            String str2 = "";
            str1 = "0000000" + value.ToString("X");
            str1 = str1.Substring(str1.Length - num, num);
            for (Int32 i = 0; i < str1.Length / 2; i++)
            {
                str2 = str2 + str1.Substring(str1.Length - 2 - 2 * i, 2);
            }
            return str2;
        }

        /// <summary>Sub ESP</summary>
        /// <param name="addre"></param>
        /// <returns></returns>
        public AsmX86 SUB_ESP(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
            {
                Builder.Append("83EC" + intTohex(addre, 2));
            }
            else
            {
                Builder.Append("81EC" + intTohex(addre, 8));
            }

            return this;
        }

        public AsmX86 Nop()
        {
            Builder.Append("90");

            return this;
        }

        public AsmX86 RetA(Int32 addre)
        {
            Builder.Append(intTohex(addre, 4));

            return this;
        }

        public AsmX86 IN_AL_DX()
        {
            Builder.Append("EC");

            return this;
        }

        public AsmX86 TEST_EAX_EAX()
        {
            Builder.Append("85C0");

            return this;
        }

        public AsmX86 Leave()
        {
            Builder.Append("C9");

            return this;
        }

        public AsmX86 Pushad()
        {
            Builder.Append("60");

            return this;
        }

        public AsmX86 Popad()
        {
            Builder.Append("61");

            return this;
        }

        public AsmX86 Ret()
        {
            Builder.Append("C3");

            return this;
        }

        #region ADD
        public AsmX86 Add_EAX_EDX()
        {
            Builder.Append("03C2");

            return this;
        }

        public void Add_EBX_EAX()
        {
            Builder.Append("03D8");
        }

        public void Add_EAX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("0305" + intTohex(addre, 8));
        }

        public void Add_EBX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("031D" + intTohex(addre, 8));
        }

        public void Add_EBP_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("032D" + intTohex(addre, 8));
        }

        public void Add_EAX(Int32 addre)
        {
            Builder.Append("05" + intTohex(addre, 8));
        }

        public void Add_EBX(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83C3" + intTohex(addre, 2));
            else
                Builder.Append("81C3" + intTohex(addre, 8));
        }

        public void Add_ECX(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83C1" + intTohex(addre, 2));
            else
                Builder.Append("81C1" + intTohex(addre, 8));
        }

        public void Add_EDX(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83C2" + intTohex(addre, 2));
            else
                Builder.Append("81C2" + intTohex(addre, 8));
        }

        public void Add_ESI(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83C6" + intTohex(addre, 2));
            else
                Builder.Append("81C6" + intTohex(addre, 8));
        }

        public void Add_ESP(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83C4" + intTohex(addre, 2));
            else
                Builder.Append("81C4" + intTohex(addre, 8));
        }
        #endregion

        #region mov

        public void Mov_DWORD_Ptr_EAX_ADD(Int32 addre, Int32 addre1)
        {
            if ((addre <= 127) && (addre >= -128))
            {
                Builder.Append("C740" + intTohex(addre, 2) + intTohex(addre1, 8));
            }
            else
            {
                Builder.Append("C780" + intTohex(addre, 8) + intTohex(addre1, 8));
            }
        }

        public void Mov_DWORD_Ptr_ESP_ADD(Int32 addre, Int32 addre1)
        {
            if ((addre <= 127) && (addre >= -128))
            {
                Builder.Append("C74424" + intTohex(addre, 2) + intTohex(addre1, 8));
            }
            else
            {
                Builder.Append("C78424" + intTohex(addre, 8) + intTohex(addre1, 8));
            }
        }

        public void Mov_DWORD_Ptr_ESP_ADD_EAX(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
            {
                Builder.Append("894424" + intTohex(addre, 2));
            }
            else
            {
                Builder.Append("898424" + intTohex(addre, 8));
            }
        }

        public void Mov_DWORD_Ptr_ESP(Int32 addre)
        {
            Builder.Append("C70424" + intTohex(addre, 8));
        }

        public void Mov_DWORD_Ptr_EAX(Int32 addre)
        {
            Builder.Append("A3" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B1D" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B0D" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("A1" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B15" + intTohex(addre, 8));
        }

        public void Mov_ESI_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B35" + intTohex(addre, 8));
        }

        public void Mov_ESP_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B25" + intTohex(addre, 8));
        }

        public void Mov_EBP_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("8B2D" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_EAX(Int32 addre)
        {
            Builder.Append("8B00");
        }

        public void Mov_EAX_DWORD_Ptr_EAX()
        {
            Builder.Append("8B00");
        }

        public void Mov_EAX_DWORD_Ptr_EBP()
        {
            Builder.Append("8B4500");
        }

        public void Mov_EAX_DWORD_Ptr_EBX()
        {
            Builder.Append("8B03");
        }

        public void Mov_EAX_DWORD_Ptr_ECX()
        {
            Builder.Append("8B01");
        }

        public void Mov_EAX_DWORD_Ptr_EDX()
        {
            Builder.Append("8B02");
        }

        public void Mov_EAX_DWORD_Ptr_EDI()
        {
            Builder.Append("8B07");
        }

        public void Mov_EAX_DWORD_Ptr_ESP()
        {
            Builder.Append("8B0424");
        }

        public void Mov_EAX_DWORD_Ptr_ESI()
        {
            Builder.Append("8B06");
        }

        public void Mov_EAX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
            {
                Builder.Append("8B40" + intTohex(addre, 2));
            }
            else
            {
                Builder.Append("8B80" + intTohex(addre, 8));
            }
        }

        public void Mov_EAX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4424" + intTohex(addre, 2));
            else
                Builder.Append("8B8424" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B43" + intTohex(addre, 2));
            else
                Builder.Append("8B83" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B41" + intTohex(addre, 2));
            else
                Builder.Append("8B81" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B42" + intTohex(addre, 2));
            else
                Builder.Append("8B82" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B47" + intTohex(addre, 2));
            else
                Builder.Append("8B87" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B45" + intTohex(addre, 2));
            else
                Builder.Append("8B85" + intTohex(addre, 8));
        }

        public void Mov_EAX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B46" + intTohex(addre, 2));
            else
                Builder.Append("8B86" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B58" + intTohex(addre, 2));
            else
                Builder.Append("8B98" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5C24" + intTohex(addre, 2));
            else
                Builder.Append("8B9C24" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5B" + intTohex(addre, 2));
            else
                Builder.Append("8B9B" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B59" + intTohex(addre, 2));
            else
                Builder.Append("8B99" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5A" + intTohex(addre, 2));
            else
                Builder.Append("8B9A" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5F" + intTohex(addre, 2));
            else
                Builder.Append("8B9F" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5D" + intTohex(addre, 2));
            else
                Builder.Append("8B9D" + intTohex(addre, 8));
        }

        public void Mov_EBX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5E" + intTohex(addre, 2));
            else
                Builder.Append("8B9E" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B48" + intTohex(addre, 2));
            else
                Builder.Append("8B88" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4C24" + intTohex(addre, 2));
            else
                Builder.Append("8B8C24" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4B" + intTohex(addre, 2));
            else
                Builder.Append("8B8B" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B49" + intTohex(addre, 2));
            else
                Builder.Append("8B89" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4A" + intTohex(addre, 2));
            else
                Builder.Append("8B8A" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4F" + intTohex(addre, 2));
            else
                Builder.Append("8B8F" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4D" + intTohex(addre, 2));
            else
                Builder.Append("8B8D" + intTohex(addre, 8));
        }

        public void Mov_ECX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B4E" + intTohex(addre, 2));
            else
                Builder.Append("8B8E" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B50" + intTohex(addre, 2));
            else
                Builder.Append("8B90" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B5424" + intTohex(addre, 2));
            else
                Builder.Append("8B9424" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B53" + intTohex(addre, 2));
            else
                Builder.Append("8B93" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B51" + intTohex(addre, 2));
            else
                Builder.Append("8B91" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B52" + intTohex(addre, 2));
            else
                Builder.Append("8B92" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B57" + intTohex(addre, 2));
            else
                Builder.Append("8B97" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B55" + intTohex(addre, 2));
            else
                Builder.Append("8B95" + intTohex(addre, 8));
        }

        public void Mov_EDX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8B56" + intTohex(addre, 2));
            else
                Builder.Append("8B96" + intTohex(addre, 8));
        }

        public void Mov_ECX_EAX()
        {
            Builder.Append("8BC8");
        }

        public void Mov_EAX(Int32 addre)
        {
            Builder.Append("B8" + intTohex(addre, 8));
        }

        public void Mov_EBX(Int32 addre)
        {
            Builder.Append("BB" + intTohex(addre, 8));
        }

        public void Mov_ECX(Int32 addre)
        {
            Builder.Append("B9" + intTohex(addre, 8));
        }

        public void Mov_EDX(Int32 addre)
        {
            Builder.Append("BA" + intTohex(addre, 8));
        }

        public void Mov_ESI(Int32 addre)
        {
            Builder.Append("BE" + intTohex(addre, 8));
        }

        public void Mov_ESP(Int32 addre)
        {
            Builder.Append("BC" + intTohex(addre, 8));
        }

        public void Mov_EBP(Int32 addre)
        {
            Builder.Append("BD" + intTohex(addre, 8));
        }

        public void Mov_EDI(Int32 addre)
        {
            Builder.Append("BF" + intTohex(addre, 8));
        }

        public void Mov_ESI_DWORD_Ptr_EAX()
        {
            Builder.Append("8B7020");
        }

        public void Mov_EBX_DWORD_Ptr_EAX()
        {
            Builder.Append("8B18");
        }

        public void Mov_EBX_DWORD_Ptr_EBP()
        {
            Builder.Append("8B5D00");
        }

        public void Mov_EBX_DWORD_Ptr_EBX()
        {
            Builder.Append("8B1B");
        }

        public void Mov_EBX_DWORD_Ptr_ECX()
        {
            Builder.Append("8B19");
        }

        public void Mov_EBX_DWORD_Ptr_EDX()
        {
            Builder.Append("8B1A");
        }

        public void Mov_EBX_DWORD_Ptr_EDI()
        {
            Builder.Append("8B1F");
        }

        public void Mov_EBX_DWORD_Ptr_ESP()
        {
            Builder.Append("8B1C24");
        }

        public void Mov_EBX_DWORD_Ptr_ESI()
        {
            Builder.Append("8B1E");
        }

        public void Mov_ECX_DWORD_Ptr_EAX()
        {
            Builder.Append("8B08");
        }

        public void Mov_ECX_DWORD_Ptr_EBP()
        {
            Builder.Append("8B4D00");
        }

        public void Mov_ECX_DWORD_Ptr_EBX()
        {
            Builder.Append("8B0B");
        }

        public void Mov_ECX_DWORD_Ptr_ECX()
        {
            Builder.Append("8B09");
        }

        public void Mov_ECX_DWORD_Ptr_EDX()
        {
            Builder.Append("8B0A");
        }

        public void Mov_ECX_DWORD_Ptr_EDI()
        {
            Builder.Append("8B0F");
        }

        public void Mov_ECX_DWORD_Ptr_ESP()
        {
            Builder.Append("8B0C24");
        }

        public void Mov_ECX_DWORD_Ptr_ESI()
        {
            Builder.Append("8B0E");
        }

        public void Mov_EDX_DWORD_Ptr_EAX()
        {
            Builder.Append("8B10");
        }

        public void Mov_EDX_DWORD_Ptr_EBP()
        {
            Builder.Append("8B5500");
        }

        public void Mov_EDX_DWORD_Ptr_EBX()
        {
            Builder.Append("8B13");
        }

        public void Mov_EDX_DWORD_Ptr_ECX()
        {
            Builder.Append("8B11");
        }

        public void Mov_EDX_DWORD_Ptr_EDX()
        {
            Builder.Append("8B12");
        }

        public void Mov_EDX_DWORD_Ptr_EDI()
        {
            Builder.Append("8B17");
        }

        public void Mov_EDX_DWORD_Ptr_ESI()
        {
            Builder.Append("8B16");
        }

        public void Mov_EDX_DWORD_Ptr_ESP()
        {
            Builder.Append("8B1424");
        }

        public void Mov_EAX_EBP()
        {
            Builder.Append("8BC5");
        }

        public void Mov_EAX_EBX()
        {
            Builder.Append("8BC3");
        }

        public void Mov_EAX_ECX()
        {
            Builder.Append("8BC1");
        }

        public void Mov_EAX_EDI()
        {
            Builder.Append("8BC7");
        }

        public void Mov_EAX_EDX()
        {
            Builder.Append("8BC2");
        }

        public void Mov_EAX_ESI()
        {
            Builder.Append("8BC6");
        }

        public void Mov_EAX_ESP()
        {
            Builder.Append("8BC4");
        }

        public void Mov_EBX_EBP()
        {
            Builder.Append("8BDD");
        }

        public void Mov_EBX_EAX()
        {
            Builder.Append("8BD8");
        }

        public void Mov_EBX_ECX()
        {
            Builder.Append("8BD9");
        }

        public void Mov_EBX_EDI()
        {
            Builder.Append("8BDF");
        }

        public void Mov_EBX_EDX()
        {
            Builder.Append("8BDA");
        }

        public void Mov_EBX_ESI()
        {
            Builder.Append("8BDE");
        }

        public void Mov_EBX_ESP()
        {
            Builder.Append("8BDC");
        }

        public void Mov_ECX_EBP()
        {
            Builder.Append("8BCD");
        }

        /* public void Mov_ECX_EAX()
          {
              Builder.Append("8BC8");
          }*/

        public void Mov_ECX_EBX()
        {
            Builder.Append("8BCB");
        }

        public void Mov_ECX_EDI()
        {
            Builder.Append("8BCF");
        }

        public void Mov_ECX_EDX()
        {
            Builder.Append("8BCA");
        }

        public void Mov_ECX_ESI()
        {
            Builder.Append("8BCE");
        }

        public void Mov_ECX_ESP()
        {
            Builder.Append("8BCC");
        }

        public void Mov_EDX_EBP()
        {
            Builder.Append("8BD5");
        }

        public void Mov_EDX_EBX()
        {
            Builder.Append("8BD3");
        }

        public void Mov_EDX_ECX()
        {
            Builder.Append("8BD1");
        }

        public void Mov_EDX_EDI()
        {
            Builder.Append("8BD7");
        }

        public void Mov_EDX_EAX()
        {
            Builder.Append("8BD0");
        }

        public void Mov_EDX_ESI()
        {
            Builder.Append("8BD6");
        }

        public void Mov_EDX_ESP()
        {
            Builder.Append("8BD4");
        }

        public void Mov_ESI_EBP()
        {
            Builder.Append("8BF5");
        }

        public void Mov_ESI_EBX()
        {
            Builder.Append("8BF3");
        }

        public void Mov_ESI_ECX()
        {
            Builder.Append("8BF1");
        }

        public void Mov_ESI_EDI()
        {
            Builder.Append("8BF7");
        }

        public void Mov_ESI_EAX()
        {
            Builder.Append("8BF0");
        }

        public void Mov_ESI_EDX()
        {
            Builder.Append("8BF2");
        }

        public void Mov_ESI_ESP()
        {
            Builder.Append("8BF4");
        }

        public void Mov_ESP_EBP()
        {
            Builder.Append("8BE5");
        }

        public void Mov_ESP_EBX()
        {
            Builder.Append("8BE3");
        }

        public void Mov_ESP_ECX()
        {
            Builder.Append("8BE1");
        }

        public void Mov_ESP_EDI()
        {
            Builder.Append("8BE7");
        }

        public void Mov_ESP_EAX()
        {
            Builder.Append("8BE0");
        }

        public void Mov_ESP_EDX()
        {
            Builder.Append("8BE2");
        }

        public void Mov_ESP_ESI()
        {
            Builder.Append("8BE6");
        }

        public void Mov_EDI_EBP()
        {
            Builder.Append("8BFD");
        }

        public void Mov_EDI_EAX()
        {
            Builder.Append("8BF8");
        }

        public void Mov_EDI_EBX()
        {
            Builder.Append("8BFB");
        }

        public void Mov_EDI_ECX()
        {
            Builder.Append("8BF9");
        }

        public void Mov_EDI_EDX()
        {
            Builder.Append("8BFA");
        }

        public void Mov_EDI_ESI()
        {
            Builder.Append("8BFE");
        }

        public void Mov_EDI_ESP()
        {
            Builder.Append("8BFC");
        }

        public void Mov_EBP_EDI()
        {
            Builder.Append("8BDF");
        }

        public void Mov_EBP_EAX()
        {
            Builder.Append("8BE8");
        }

        public void Mov_EBP_EBX()
        {
            Builder.Append("8BEB");
        }

        public void Mov_EBP_ECX()
        {
            Builder.Append("8BE9");
        }

        public void Mov_EBP_EDX()
        {
            Builder.Append("8BEA");
        }

        public void Mov_EBP_ESI()
        {
            Builder.Append("8BEE");
        }

        public void Mov_EBP_ESP()
        {
            Builder.Append("8BEC");
        }
        #endregion

        #region Push
        public void Push68(Int32 addre)
        {
            Builder.Append("68" + intTohex(addre, 8));

        }

        public void Push6A(Int32 addre)
        {
            Builder.Append("6A" + intTohex(addre, 2));
        }

        public void Push_EAX()
        {
            Builder.Append("50");
        }

        public void Push_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("FF35" + intTohex(addre, 8));
        }

        public void Push_ECX()
        {
            Builder.Append("51");
        }

        public void Push_EDX()
        {
            Builder.Append("52");
        }

        public void Push_EBX()
        {
            Builder.Append("53");
        }

        public void Push_ESP()
        {
            Builder.Append("54");
        }

        public void Push_EBP()
        {
            Builder.Append("55");
        }

        public void Push_ESI()
        {
            Builder.Append("56");
        }

        public void Push_EDI()
        {
            Builder.Append("57");
        }
        #endregion

        #region Call
        public void Call_EAX()
        {
            Builder.Append("FFD0");
        }

        public void Call_EBX()
        {
            Builder.Append("FFD3");
        }

        public void Call_ECX()
        {
            Builder.Append("FFD1");
        }

        public void Call_EDX()
        {
            Builder.Append("FFD2");
        }

        public void Call_ESI()
        {
            Builder.Append("FFD2");
        }

        public void Call_ESP()
        {
            Builder.Append("FFD4");
        }

        public void Call_EBP()
        {
            Builder.Append("FFD5");
        }

        public void Call_EDI()
        {
            Builder.Append("FFD7");
        }

        public void Call_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("FF15" + intTohex(addre, 8));
        }

        public void Call_DWORD_Ptr_EAX()
        {
            Builder.Append("FF10");
        }

        public void Call_DWORD_Ptr_EBX()
        {
            Builder.Append("FF13");
        }
        #endregion

        #region Lea
        public void Lea_EAX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D40" + intTohex(addre, 2));
            else
                Builder.Append("8D80" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D43" + intTohex(addre, 2));
            else
                Builder.Append("8D83" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D41" + intTohex(addre, 2));
            else
                Builder.Append("8D81" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D42" + intTohex(addre, 2));
            else
                Builder.Append("8D82" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D46" + intTohex(addre, 2));
            else
                Builder.Append("8D86" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D40" + intTohex(addre, 2));
            else
                Builder.Append("8D80" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4424" + intTohex(addre, 2));
            else
                Builder.Append("8D8424" + intTohex(addre, 8));
        }

        public void Lea_EAX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D47" + intTohex(addre, 2));
            else
                Builder.Append("8D87" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D58" + intTohex(addre, 2));
            else
                Builder.Append("8D98" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5C24" + intTohex(addre, 2));
            else
                Builder.Append("8D9C24" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5B" + intTohex(addre, 2));
            else
                Builder.Append("8D9B" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D59" + intTohex(addre, 2));
            else
                Builder.Append("8D99" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5A" + intTohex(addre, 2));
            else
                Builder.Append("8D9A" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5F" + intTohex(addre, 2));
            else
                Builder.Append("8D9F" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5D" + intTohex(addre, 2));
            else
                Builder.Append("8D9D" + intTohex(addre, 8));
        }

        public void Lea_EBX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5E" + intTohex(addre, 2));
            else
                Builder.Append("8D9E" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D48" + intTohex(addre, 2));
            else
                Builder.Append("8D88" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4C24" + intTohex(addre, 2));
            else
                Builder.Append("8D8C24" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4B" + intTohex(addre, 2));
            else
                Builder.Append("8D8B" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D49" + intTohex(addre, 2));
            else
                Builder.Append("8D89" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4A" + intTohex(addre, 2));
            else
                Builder.Append("8D8A" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4F" + intTohex(addre, 2));
            else
                Builder.Append("8D8F" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4D" + intTohex(addre, 2));
            else
                Builder.Append("8D8D" + intTohex(addre, 8));
        }

        public void Lea_ECX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D4E" + intTohex(addre, 2));
            else
                Builder.Append("8D8E" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_EAX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D50" + intTohex(addre, 2));
            else
                Builder.Append("8D90" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_ESP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D5424" + intTohex(addre, 2));
            else
                Builder.Append("8D9424" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_EBX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D53" + intTohex(addre, 2));
            else
                Builder.Append("8D93" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_ECX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D51" + intTohex(addre, 2));
            else
                Builder.Append("8D91" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_EDX_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D52" + intTohex(addre, 2));
            else
                Builder.Append("8D92" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_EDI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D57" + intTohex(addre, 2));
            else
                Builder.Append("8D97" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_EBP_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D55" + intTohex(addre, 2));
            else
                Builder.Append("8D95" + intTohex(addre, 8));
        }

        public void Lea_EDX_DWORD_Ptr_ESI_Add(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("8D56" + intTohex(addre, 2));
            else
                Builder.Append("8D96" + intTohex(addre, 8));
        }
        #endregion

        #region POP
        public void Pop_EAX()
        {
            Builder.Append("58");
        }

        public void Pop_EBX()
        {
            Builder.Append("5B");
        }

        public void Pop_ECX()
        {
            Builder.Append("59");
        }

        public void Pop_EDX()
        {
            Builder.Append("5A");
        }

        public void Pop_ESI()
        {
            Builder.Append("5E");
        }

        public void Pop_ESP()
        {
            Builder.Append("5C");
        }

        public void Pop_EDI()
        {
            Builder.Append("5F");
        }

        public void Pop_EBP()
        {
            Builder.Append("5D");
        }
        #endregion

        #region CMP
        public void Cmp_EAX(Int32 addre)
        {
            if ((addre <= 127) && (addre >= -128))
                Builder.Append("83F8" + intTohex(addre, 2));
            else
                Builder.Append("3D" + intTohex(addre, 8));
        }

        public void Cmp_EAX_EDX()
        {
            Builder.Append("3BC2");
        }

        public void Cmp_EAX_DWORD_Ptr(Int32 addre)
        {
            Builder.Append("3B05" + intTohex(addre, 8));
        }

        public void Cmp_DWORD_Ptr_EAX(Int32 addre)
        {
            Builder.Append("3905" + intTohex(addre, 8));
        }
        #endregion

        #region DEC
        public void Dec_EAX()
        {
            Builder.Append("48");
        }

        public void Dec_EBX()
        {
            Builder.Append("4B");
        }

        public void Dec_ECX()
        {
            Builder.Append("49");
        }

        public void Dec_EDX()
        {
            Builder.Append("4A");
        }
        #endregion

        #region idiv
        public void Idiv_EAX()
        {
            Builder.Append("F7F8");
        }

        public void Idiv_EBX()
        {
            Builder.Append("F7FB");
        }

        public void Idiv_ECX()
        {
            Builder.Append("F7F9");
        }

        public void Idiv_EDX()
        {
            Builder.Append("F7FA");
        }
        #endregion

        #region Imul
        public void Imul_EAX_EDX()
        {
            Builder.Append("0FAFC2");
        }

        public void Imul_EAX(Int32 addre)
        {
            Builder.Append("6BC0" + intTohex(addre, 2));
        }

        public void ImulB_EAX(Int32 addre)
        {
            Builder.Append("69C0" + intTohex(addre, 8));
        }
        #endregion

        #region Inc
        public void Inc_EAX()
        {
            Builder.Append("40");
        }

        public void Inc_EBX()
        {
            Builder.Append("43");
        }

        public void Inc_ECX()
        {
            Builder.Append("41");
        }

        public void Inc_EDX()
        {
            Builder.Append("42");
        }

        public void Inc_EDI()
        {
            Builder.Append("47");
        }

        public void Inc_ESI()
        {
            Builder.Append("46");
        }

        public void Inc_DWORD_Ptr_EAX()
        {
            Builder.Append("FF00");
        }

        public void Inc_DWORD_Ptr_EBX()
        {
            Builder.Append("FF03");
        }

        public void Inc_DWORD_Ptr_ECX()
        {
            Builder.Append("FF01");
        }

        public void Inc_DWORD_Ptr_EDX()
        {
            Builder.Append("FF02");
        }
        #endregion

        #region jmp
        /// <summary>跳转</summary>
        public void JMP_EAX()
        {
            Builder.Append("FFE0");
        }
        #endregion

        /// <summary>在目标进程上执行</summary>
        /// <param name="pid"></param>
        public void Run(Int32 pid)
        {
            var asm = DataHelper.FromHex(Builder.ToString());
            if (pid != 0)
            {
                var hwnd = OpenProcess(PROCESS_ALL_ACCESS | PROCESS_CREATE_THREAD | PROCESS_VM_WRITE, 0, pid);
                if (hwnd != 0)
                {
                    var addre = VirtualAllocEx(hwnd, 0, asm.Length, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
                    WriteProcessMemory(hwnd, addre, asm, asm.Length, 0);

                    var threadhwnd = CreateRemoteThread(hwnd, 0, 0, addre, 0, 0, ref pid);
                    VirtualFreeEx(hwnd, addre, asm.Length, MEM_RELEASE);
                    CloseHandle(threadhwnd);
                    CloseHandle(hwnd);
                }
            }
            Builder.Length = 0;
        }
    }
}