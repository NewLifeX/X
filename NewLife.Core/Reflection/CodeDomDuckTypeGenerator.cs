using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;

namespace NewLife.Reflection
{
    class CodeDomDuckTypeGenerator
    {
        public Type[] CreateDuckTypes(Type interfaceType, Type[] duckedTypes)
        {
            const string TYPE_PREFIX = "Duck";

            String namespaceName = this.GetType().Namespace + "." + interfaceType.Name;

            CodeCompileUnit codeCU = new CodeCompileUnit();
            CodeNamespace codeNsp = new CodeNamespace(namespaceName);
            codeCU.Namespaces.Add(codeNsp);

            //CodeTypeReference codeTRInterface = new CodeTypeReference(interfaceType);
            CodeTypeReference codeTRInterface = new CodeTypeReference(TypeX.Create(interfaceType).FullName);
            ReferenceList references = new ReferenceList();

            // 遍历处理每一个需要代理的类
            for (int i = 0; i < duckedTypes.Length; i++)
            {
                Type objectType = duckedTypes[i];

                //CodeTypeReference codeTRObject = new CodeTypeReference(objectType);
                CodeTypeReference codeTRObject = new CodeTypeReference(TypeX.Create(objectType).FullName);
                references.AddReference(objectType);

                CodeTypeDeclaration codeType = new CodeTypeDeclaration(TYPE_PREFIX + i);
                codeNsp.Types.Add(codeType);

                codeType.TypeAttributes = TypeAttributes.Public;
                codeType.BaseTypes.Add(codeTRInterface);

                // 声明一个字段
                CodeMemberField codeFldObj = new CodeMemberField(codeTRObject, "_obj");
                codeType.Members.Add(codeFldObj);
                CodeFieldReferenceExpression codeFldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), codeFldObj.Name);

                // 创建一个构造函数
                CodeConstructor codeCtor = new CodeConstructor();
                codeType.Members.Add(codeCtor);
                codeCtor.Attributes = MemberAttributes.Public;
                codeCtor.Parameters.Add(new CodeParameterDeclarationExpression(codeTRObject, "obj"));
                codeCtor.Statements.Add(
                    new CodeAssignStatement(
                        codeFldRef,
                        new CodeArgumentReferenceExpression("obj")
                    )
                );

                // 创建成员
                CreateMember(interfaceType, objectType, codeType, references, codeFldRef);
            }

            #region 编译
            CSharpCodeProvider codeprov = new CSharpCodeProvider();

#if DEBUG
            {
                StringWriter sw = new StringWriter();
                codeprov.GenerateCodeFromCompileUnit(codeCU, sw, new CodeGeneratorOptions());
                string code = sw.ToString();
                Console.WriteLine(code);
            }
#endif

            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateInMemory = true;
            compilerParams.ReferencedAssemblies.Add(interfaceType.Assembly.Location);

            references.SetToCompilerParameters(compilerParams);

            CompilerResults cres = codeprov.CompileAssemblyFromDom(compilerParams, codeCU);
            if (cres.Errors.Count > 0)
            {
                StringWriter sw = new StringWriter();
                foreach (CompilerError err in cres.Errors)
                    sw.WriteLine(err.ErrorText);

                throw new InvalidOperationException("编译错误: \n\n" + sw.ToString());
            }

            Assembly assembly = cres.CompiledAssembly;

            Type[] res = new Type[duckedTypes.Length];
            for (int i = 0; i < duckedTypes.Length; i++)
            {
                res[i] = assembly.GetType(namespaceName + "." + TYPE_PREFIX + i);
            }

            return res;
            #endregion
        }

        void CreateMember(Type interfaceType, Type duckType, CodeTypeDeclaration codeType, ReferenceList references, CodeFieldReferenceExpression codeFldRef)
        {
            CodeTypeReference codeTRInterface = new CodeTypeReference(TypeX.Create(interfaceType).FullName);

            //// 找到duckType里面是否有公共的_obj;
            //FieldInfo fiObj = duckType.GetField("_obj", BindingFlags.Public | BindingFlags.Instance);
            //Type innerType = fiObj != null ? fiObj.FieldType : null;

            CodeFieldReferenceExpression fdRef = null;

            #region 方法
            foreach (MethodInfo mi in interfaceType.GetMethods())
            {
                // 忽略专用名字的方法，如属性的get/set，还有构造函数
                if ((mi.Attributes & MethodAttributes.SpecialName) != 0) continue;

                CodeMemberMethod codeMethod = new CodeMemberMethod();
                codeType.Members.Add(codeMethod);

                codeMethod.Name = mi.Name;
                codeMethod.ReturnType = new CodeTypeReference(mi.ReturnType);
                codeMethod.PrivateImplementationType = codeTRInterface;

                references.AddReference(mi.ReturnType);

                ParameterInfo[] parameters = mi.GetParameters();
                CodeArgumentReferenceExpression[] codeArgs = new CodeArgumentReferenceExpression[parameters.Length];

                int n = 0;
                Type[] pits = new Type[parameters.Length];
                foreach (ParameterInfo parameter in parameters)
                {
                    pits[n] = parameter.ParameterType;

                    references.AddReference(parameter.ParameterType);
                    CodeParameterDeclarationExpression codeParam = new CodeParameterDeclarationExpression(parameter.ParameterType, parameter.Name);
                    codeMethod.Parameters.Add(codeParam);
                    codeArgs[n++] = new CodeArgumentReferenceExpression(parameter.Name);
                }

                CodeMethodInvokeExpression codeMethodInvoke = new CodeMethodInvokeExpression(FindMember(duckType, mi, codeFldRef), mi.Name, codeArgs);

                if (mi.ReturnType == typeof(void))
                    codeMethod.Statements.Add(codeMethodInvoke);
                else
                    codeMethod.Statements.Add(new CodeMethodReturnStatement(codeMethodInvoke));
            }
            #endregion

            #region 属性
            foreach (PropertyInfo pi in interfaceType.GetProperties())
            {
                CodeMemberProperty property = new CodeMemberProperty();
                codeType.Members.Add(property);

                property.Name = pi.Name;
                property.Type = new CodeTypeReference(pi.PropertyType);
                property.Attributes = MemberAttributes.Public;
                property.PrivateImplementationType = codeTRInterface;

                references.AddReference(pi.PropertyType);

                ParameterInfo[] parameters = pi.GetIndexParameters();
                CodeArgumentReferenceExpression[] args = new CodeArgumentReferenceExpression[parameters.Length];

                int n = 0;
                foreach (ParameterInfo parameter in parameters)
                {
                    CodeParameterDeclarationExpression codeParam = new CodeParameterDeclarationExpression(parameter.ParameterType, parameter.Name);
                    property.Parameters.Add(codeParam);

                    references.AddReference(parameter.ParameterType);

                    CodeArgumentReferenceExpression codeArgRef = new CodeArgumentReferenceExpression(parameter.Name);
                    args[n++] = codeArgRef;
                }

                fdRef = FindMember(duckType, pi, codeFldRef);

                if (pi.CanRead)
                {
                    property.HasGet = true;

                    if (args.Length == 0)
                    {
                        property.GetStatements.Add(
                            new CodeMethodReturnStatement(
                                new CodePropertyReferenceExpression(
                                    fdRef,
                                    pi.Name
                                )
                            )
                        );
                    }
                    else
                    {
                        property.GetStatements.Add(
                            new CodeMethodReturnStatement(
                                new CodeIndexerExpression(
                                    fdRef,
                                    args
                                )
                            )
                        );
                    }
                }

                if (pi.CanWrite)
                {
                    property.HasSet = true;

                    if (args.Length == 0)
                    {
                        property.SetStatements.Add(
                            new CodeAssignStatement(
                                new CodePropertyReferenceExpression(
                                    fdRef,
                                    pi.Name
                                ),
                                new CodePropertySetValueReferenceExpression()
                            )
                        );
                    }
                    else
                    {
                        property.SetStatements.Add(
                            new CodeAssignStatement(
                                new CodeIndexerExpression(
                                    fdRef,
                                    args
                                ),
                                new CodePropertySetValueReferenceExpression()
                            )
                        );
                    }

                }
            }
            #endregion

            #region 事件
            foreach (EventInfo ei in interfaceType.GetEvents())
            {
                fdRef = FindMember(duckType, ei, codeFldRef);

                StringBuilder sbCode = new StringBuilder();
                sbCode.Append("public event " + ei.EventHandlerType.FullName + " @" + ei.Name + "{");
                //sbCode.Append("add    {" + codeFldObj.Name + "." + ei.Name + "+=value;}");
                //sbCode.Append("remove {" + codeFldObj.Name + "." + ei.Name + "-=value;}");
                if (fdRef == codeFldRef)
                {
                    sbCode.Append("add    {" + codeFldRef.FieldName + "." + ei.Name + "+=value;}");
                    sbCode.Append("remove {" + codeFldRef.FieldName + "." + ei.Name + "-=value;}");
                }
                else
                {
                    sbCode.Append("add    {" + fdRef.FieldName + "." + codeFldRef.FieldName + "." + ei.Name + "+=value;}");
                    sbCode.Append("remove {" + fdRef.FieldName + "." + codeFldRef.FieldName + "." + ei.Name + "-=value;}");
                }
                sbCode.Append("}");

                references.AddReference(ei.EventHandlerType);

                codeType.Members.Add(new CodeSnippetTypeMember(sbCode.ToString()));
            }
            #endregion

            #region 递归基接口
            Type[] ts = interfaceType.GetInterfaces();
            if (ts != null && ts.Length > 0)
            {
                foreach (Type item in ts)
                {
                    CreateMember(item, duckType, codeType, references, codeFldRef);
                }
            }
            #endregion
        }

        CodeFieldReferenceExpression FindMember(Type duckType, MemberInfo mi, CodeFieldReferenceExpression codeFldRef)
        {
            MemberInfo[] infos = duckType.GetMember(mi.Name);
            if (infos != null && infos.Length > 0)
                return codeFldRef;
            else
            {
                // 找到duckType里面是否有公共的_obj;
                FieldInfo fiObj = duckType.GetField("_obj", BindingFlags.Public | BindingFlags.Instance);
                if (fiObj != null)
                {
                    Type innerType = fiObj.FieldType;
                    if (mi.DeclaringType.IsAssignableFrom(innerType)) return new CodeFieldReferenceExpression(codeFldRef, fiObj.Name);
                }
            }

            return codeFldRef;
        }

        class ReferenceList
        {
            List<string> _lst = new List<string>();

            static readonly Assembly mscorlib = typeof(object).Assembly;

            public bool AddReference(Assembly assembly)
            {
                if (!_lst.Contains(assembly.Location) && assembly != mscorlib)
                {
                    _lst.Add(assembly.Location);
                    return true;
                }

                return false;
            }

            public void AddReference(Type type)
            {
                AddReference(type.Assembly);
                if (type.BaseType != null && type.BaseType.Assembly != mscorlib)
                    AddReference(type.BaseType);
            }

            public void SetToCompilerParameters(CompilerParameters parameters)
            {
                foreach (string reference in _lst)
                    parameters.ReferencedAssemblies.Add(reference);
            }
        }
    }
}