using System;
using System.IO;
using System.Reflection;

namespace NewLife.Reflection
{
    class OrcasNamer
    {
        public static string GetName(MemberInfo member)
        {
            using (TextWriter writer = new StringWriter())
            {
                switch (member.MemberType)
                {
                    case MemberTypes.TypeInfo:
                    case MemberTypes.NestedType:
                        writer.Write("T:");
                        WriteType(member as Type, writer);
                        break;
                    case MemberTypes.Field:
                        writer.Write("F:");
                        WriteField(member as FieldInfo, writer);
                        break;
                    case MemberTypes.Property:
                        writer.Write("P:");
                        WriteProperty(member as PropertyInfo, writer);
                        break;
                    case MemberTypes.Method:
                        writer.Write("M:");
                        WriteMethod(member as MethodInfo, writer);
                        break;
                    case MemberTypes.Constructor:
                        writer.Write("M:");
                        ConstructorInfo ctor = member as ConstructorInfo;
                        if (!ctor.IsStatic)
                            WriteConstructor(ctor, writer);
                        else
                            WriteStaticConstructor(ctor, writer);
                        break;
                    case MemberTypes.Event:
                        writer.Write("E:");
                        WriteEvent(member as EventInfo, writer);
                        break;
                }

                return writer.ToString();
            }
        }

        private static void WriteEvent(EventInfo trigger, TextWriter writer)
        {
            WriteType(trigger.DeclaringType, writer);

            //EventInfo eiiTrigger = null;
            //if (trigger.IsPrivate && trigger.IsVirtual)
            //{
            //    EventInfo[] eiiTriggers = ReflectionUtilities.GetImplementedEvents(trigger);
            //    if (eiiTriggers.Length > 0) eiiTrigger = eiiTriggers[0];
            //}

            //if (eiiTrigger != null)
            //{
            //    Type eiiType = eiiTrigger.DeclaringType;
            //    TextWriter eiiWriter = new StringWriter();

            //    if (eiiType != null && eiiType.Template != null)
            //    {
            //        writer.Write(".");
            //        WriteTemplate(eiiType, writer);
            //    }
            //    else
            //    {
            //        WriteType(eiiType, eiiWriter);
            //        writer.Write(".");
            //        writer.Write(eiiWriter.ToString().Replace('.', '#'));
            //    }

            //    writer.Write("#");
            //    writer.Write(eiiTrigger.Name);
            //}
            //else
            {
                writer.Write(".{0}", trigger.Name);
            }
        }

        private static void WriteField(FieldInfo field, TextWriter writer)
        {
            WriteType(field.DeclaringType, writer);
            writer.Write(".{0}", field.Name);
        }

        private static void WriteMethod(MethodInfo method, TextWriter writer)
        {
            string name = method.Name;
            WriteType(method.DeclaringType, writer);

            //MethodInfo eiiMethod = null;
            //if (method.IsPrivate && method.IsVirtual)
            //{
            //    MethodInfo[] eiiMethods = method.ImplementedInterfaceMethods;
            //    if (eiiMethods.Length > 0) eiiMethod = eiiMethods[0];
            //}
            //if (eiiMethod != null)
            //{ //explicitly implemented interface
            //    Type eiiType = eiiMethod.DeclaringType;
            //    TextWriter eiiWriter = new StringWriter();


            //    //we need to keep the param names instead of turning them into numbers
            //    //get the template to the right format
            //    if (eiiType != null && eiiType.Template != null)
            //    {
            //        writer.Write(".");
            //        WriteTemplate(eiiType, writer);
            //    }
            //    else //revert back to writing the type the old way if there is no template
            //    {
            //        WriteType(eiiType, eiiWriter);
            //        writer.Write(".");
            //        writer.Write(eiiWriter.ToString().Replace('.', '#'));
            //    }

            //    writer.Write("#");
            //    writer.Write(eiiMethod.Name);
            //}
            //else
            {
                writer.Write(".{0}", name);
            }
            if (method.IsGenericMethod)
            {
                Type[] genericParameters = method.GetGenericArguments();
                if (genericParameters != null)
                {
                    writer.Write("``{0}", genericParameters.Length);
                }
            }
            WriteParameters(method.GetParameters(), writer);
            // add ~ for conversion operators
            if ((name == "op_Implicit") || (name == "op_Explicit"))
            {
                writer.Write("~");
                WriteType(method.ReturnType, writer);
            }
        }

        private static void WriteConstructor(ConstructorInfo constructor, TextWriter writer)
        {
            WriteType(constructor.DeclaringType, writer);
            writer.Write(".#ctor");
            WriteParameters(constructor.GetParameters(), writer);
        }

        private static void WriteStaticConstructor(ConstructorInfo constructor, TextWriter writer)
        {
            WriteType(constructor.DeclaringType, writer);
            writer.Write(".#cctor");
            WriteParameters(constructor.GetParameters(), writer);
        }

        private static void WriteProperty(PropertyInfo property, TextWriter writer)
        {
            WriteType(property.DeclaringType, writer);
            //Console.WriteLine( "{0}::{1}", property.DeclaringType.FullName, property.Name );

            //Property eiiProperty = null;
            //if (property.IsPrivate && property.IsVirtual)
            //{
            //    Property[] eiiProperties = ReflectionUtilities.GetImplementedProperties(property);
            //    if (eiiProperties.Length > 0) eiiProperty = eiiProperties[0];
            //}



            //if (eiiProperty != null)
            //{
            //    TypeNode eiiType = eiiProperty.DeclaringType;
            //    TextWriter eiiWriter = new StringWriter();


            //    if (eiiType != null && eiiType.Template != null)
            //    {
            //        writer.Write(".");
            //        WriteTemplate(eiiType, writer);
            //    }
            //    else
            //    {
            //        WriteType(eiiType, eiiWriter);
            //        writer.Write(".");
            //        writer.Write(eiiWriter.ToString().Replace('.', '#'));
            //    }

            //    writer.Write("#");
            //    writer.Write(eiiProperty.Name.Name);
            //}
            //else
            {
                writer.Write(".{0}", property.Name);
            }
            WriteParameters(property.GetIndexParameters(), writer);
        }

        private static void WriteParameters(ParameterInfo[] parameters, TextWriter writer)
        {
            if (parameters == null || parameters.Length == 0) return;
            writer.Write("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) writer.Write(",");
                WriteType(parameters[i].ParameterType, writer);
            }
            writer.Write(")");
        }

        private static void WriteType(Type type, TextWriter writer)
        {
            if (type.IsArray)
            {
                WriteType(type.GetElementType(), writer);
                writer.Write("[");
                if (type.GetArrayRank() > 1)
                {
                    for (int i = 0; i < type.GetArrayRank(); i++)
                    {
                        if (i > 0) writer.Write(",");
                        writer.Write("0:");
                    }
                }
                writer.Write("]");
            }
            else if (type.IsByRef)
            {
                WriteType(type.GetElementType(), writer);
                writer.Write("@");
            }
            else if (type.IsPointer)
            {
                WriteType(type.GetElementType(), writer);
                writer.Write("*");
            }
            //case MemberTypes.OptionalModifier:
            //    TypeModifier optionalModifierClause = type as TypeModifier;
            //    WriteType(optionalModifierClause.ModifiedType, writer);
            //    writer.Write("!");
            //    WriteType(optionalModifierClause.Modifier, writer);
            //    break;
            //case MemberTypes.RequiredModifier:
            //    TypeModifier requiredModifierClause = type as TypeModifier;
            //    WriteType(requiredModifierClause.ModifiedType, writer);
            //    writer.Write("|");
            //    WriteType(requiredModifierClause.Modifier, writer);
            //    break;
            else
            {
                if (type.IsGenericParameter)
                {
                    if (type.DeclaringMethod != null)
                        writer.Write("``");
                    else if (type.DeclaringType != null)
                        writer.Write("`");
                    else
                        throw new InvalidOperationException("Generic parameter not on type or method.");
                    writer.Write(type.GenericParameterPosition);
                }
                else
                {
                    // namespace
                    Type declaringType = type.DeclaringType;
                    if (declaringType != null)
                    {
                        // names of nested types begin with outer type name
                        WriteType(declaringType, writer);
                        writer.Write(".");
                    }
                    else
                    {
                        // otherwise just prepend the namespace
                        //Identifier space = type.Namespace;
                        if (!String.IsNullOrEmpty(type.Namespace))
                        {
                            writer.Write(type.Namespace);
                            writer.Write(".");
                        }
                    }
                    // name
                    //writer.Write(type.GetUnmangledNameWithoutTypeParameters());
                    String typeName = type.Name;
                    if (typeName.Contains("`"))
                        writer.Write(typeName.Substring(0, typeName.IndexOf("`")));
                    else
                        writer.Write(typeName);
                    // generic parameters
                    if (type.IsGenericType)
                    {
                        if (type.IsGenericTypeDefinition)
                        {
                            // number of parameters
                            Type[] parameters = type.GetGenericArguments();
                            if (parameters != null)
                            {
                                writer.Write("`{0}", parameters.Length);
                            }
                        }
                        else
                        {
                            // arguments
                            Type[] arguments = type.GetGenericArguments();
                            if (arguments != null && arguments.Length > 0)
                            {
                                writer.Write("{");
                                for (int i = 0; i < arguments.Length; i++)
                                {
                                    //TypeNode argument = arguments[i];
                                    if (i > 0) writer.Write(",");
                                    WriteType(arguments[i], writer);
                                }
                                writer.Write("}");
                            }
                        }
                    }
                }
            }
        }
    }
}