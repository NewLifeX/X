#define DEBUG_GENERATED_CODE // uncommend this line to print the generated code to the console

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;

namespace NewLife.Reflection
{
    class CodeDomDuckTypeGenerator
    {
        public Type[] CreateDuckTypes(Type interfaceType, Type[] duckedTypes)
        {
            const string TYPE_PREFIX = "Duck";
            const string COMMON_NAMESPACE = "DynamicDucks";

            Type[] ret = new Type[duckedTypes.Length];

            string namespaceName = COMMON_NAMESPACE + "." + interfaceType.Name;

            CodeCompileUnit codeCU = new CodeCompileUnit();
            CodeNamespace codeNsp = new CodeNamespace(namespaceName);
            codeCU.Namespaces.Add(codeNsp);

            //CodeTypeReference codeTRInterface = new CodeTypeReference(interfaceType);
            CodeTypeReference codeTRInterface = new CodeTypeReference(TypeX.Create(interfaceType).FullName);
            ReferenceList references = new ReferenceList();

            for (int i = 0; i < duckedTypes.Length; i++)
            {
                Type objectType = duckedTypes[i];

                //CodeTypeReference codeTRObject = new CodeTypeReference(objectType);
                CodeTypeReference codeTRObject = new CodeTypeReference(TypeX.Create(objectType).FullName);
                references.AddReference(objectType);

                CodeTypeDeclaration codeType = new CodeTypeDeclaration(TYPE_PREFIX + i.ToString());
                codeNsp.Types.Add(codeType);

                codeType.TypeAttributes = TypeAttributes.Public;
                codeType.BaseTypes.Add(codeTRInterface);

                CodeMemberField codeFldObj = new CodeMemberField(codeTRObject, "_obj");
                codeType.Members.Add(codeFldObj);
                CodeFieldReferenceExpression codeFldRef = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), codeFldObj.Name);

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

                #region Implement Methods
                foreach (MethodInfo miInterface in interfaceType.GetMethods())
                {
                    if ((miInterface.Attributes & MethodAttributes.SpecialName) != 0) continue; // ignore set_PROPERTY and get_PROPERTY methods

                    CodeMemberMethod codeMethod = new CodeMemberMethod();
                    codeType.Members.Add(codeMethod);

                    codeMethod.Name = miInterface.Name;
                    codeMethod.ReturnType = new CodeTypeReference(miInterface.ReturnType);
                    codeMethod.PrivateImplementationType = codeTRInterface;

                    references.AddReference(miInterface.ReturnType);

                    ParameterInfo[] parameters = miInterface.GetParameters();
                    CodeArgumentReferenceExpression[] codeArgs = new CodeArgumentReferenceExpression[parameters.Length];

                    int n = 0;
                    foreach (ParameterInfo parameter in parameters)
                    {
                        references.AddReference(parameter.ParameterType);
                        CodeParameterDeclarationExpression codeParam = new CodeParameterDeclarationExpression(parameter.ParameterType, parameter.Name);
                        codeMethod.Parameters.Add(codeParam);
                        codeArgs[n++] = new CodeArgumentReferenceExpression(parameter.Name);
                    }

                    CodeMethodInvokeExpression codeMethodInvoke = new CodeMethodInvokeExpression(codeFldRef, miInterface.Name, codeArgs);
                    if (miInterface.ReturnType == typeof(void))
                    {
                        codeMethod.Statements.Add(codeMethodInvoke);
                    }
                    else
                    {
                        codeMethod.Statements.Add(new CodeMethodReturnStatement(codeMethodInvoke));
                    }
                }
                #endregion

                #region Implement Properties
                foreach (PropertyInfo piInterface in interfaceType.GetProperties())
                {

                    CodeMemberProperty property = new CodeMemberProperty();
                    codeType.Members.Add(property);

                    property.Name = piInterface.Name;
                    property.Type = new CodeTypeReference(piInterface.PropertyType);
                    property.Attributes = MemberAttributes.Public;
                    property.PrivateImplementationType = new CodeTypeReference(interfaceType);

                    references.AddReference(piInterface.PropertyType);

                    ParameterInfo[] parameters = piInterface.GetIndexParameters();
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

                    if (piInterface.CanRead)
                    {
                        property.HasGet = true;

                        if (args.Length == 0)
                        {
                            property.GetStatements.Add(
                                new CodeMethodReturnStatement(
                                    new CodePropertyReferenceExpression(
                                        codeFldRef,
                                        piInterface.Name
                                    )
                                )
                            );
                        }
                        else
                        {
                            property.GetStatements.Add(
                                new CodeMethodReturnStatement(
                                    new CodeIndexerExpression(
                                        codeFldRef,
                                        args
                                    )
                                )
                            );
                        }
                    }

                    if (piInterface.CanWrite)
                    {
                        property.HasSet = true;

                        if (args.Length == 0)
                        {
                            property.SetStatements.Add(
                                new CodeAssignStatement(
                                    new CodePropertyReferenceExpression(
                                        codeFldRef,
                                        piInterface.Name
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
                                        codeFldRef,
                                        args
                                    ),
                                    new CodePropertySetValueReferenceExpression()
                                )
                            );
                        }

                    }
                }

                #endregion

                #region Implement Events
                foreach (EventInfo eiEvent in interfaceType.GetEvents())
                {

                    // no declaration of Events (including custom add / remove handlers) is supported by CodeDom
                    // a simplet has to be used

                    StringBuilder sbCode = new StringBuilder();
                    sbCode.Append("public event " + eiEvent.EventHandlerType.FullName + " @" + eiEvent.Name + "{");
                    sbCode.Append("add    {" + codeFldObj.Name + "." + eiEvent.Name + "+=value;}");
                    sbCode.Append("remove {" + codeFldObj.Name + "." + eiEvent.Name + "-=value;}");
                    sbCode.Append("}");

                    references.AddReference(eiEvent.EventHandlerType);

                    codeType.Members.Add(new CodeSnippetTypeMember(sbCode.ToString()));
                }
                #endregion
            }

            Microsoft.CSharp.CSharpCodeProvider codeprov = new Microsoft.CSharp.CSharpCodeProvider();

#if DEBUG_GENERATED_CODE
            System.IO.StringWriter sw = new System.IO.StringWriter();
            codeprov.GenerateCodeFromCompileUnit(codeCU, sw, new CodeGeneratorOptions());
            string code = sw.ToString();
            Console.WriteLine(code);
#endif

            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateInMemory = true;
            compilerParams.ReferencedAssemblies.Add(interfaceType.Assembly.Location);

            references.SetToCompilerParameters(compilerParams);

            CompilerResults cres = codeprov.CompileAssemblyFromDom(compilerParams, codeCU);
            if (cres.Errors.Count > 0)
            {
                StringWriter swErrors = new StringWriter();
                foreach (CompilerError err in cres.Errors)
                    swErrors.WriteLine(err.ErrorText);

                throw new Exception("Compiler-Errors: \n\n" + swErrors.ToString());
            }

            Assembly assembly = cres.CompiledAssembly;

            Type[] res = new Type[duckedTypes.Length];
            for (int i = 0; i < duckedTypes.Length; i++)
            {
                res[i] = assembly.GetType(namespaceName + "." + TYPE_PREFIX + i.ToString());
            }

            return res;
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
                if (type.BaseType.Assembly != mscorlib)
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