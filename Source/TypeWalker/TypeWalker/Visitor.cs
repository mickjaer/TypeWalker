﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TypeWalker.Extensions;
using TypeWalker.Generators;

namespace TypeWalker
{
    public class Visitor
    {
        public event EventHandler<NameSpaceEventArgs> NameSpaceVisiting;
        public event EventHandler<NameSpaceEventArgs> NameSpaceVisited;
        public event EventHandler<MemberEventArgs> MemberVisiting;
        public event EventHandler<MemberEventArgs> MemberVisited;
        public event EventHandler<MethodEventArgs> MethodVisiting;
        public event EventHandler<MethodEventArgs> MethodVisited;
        public event EventHandler<TypeEventArgs> TypeVisiting;
        public event EventHandler<TypeEventArgs> TypeVisited;

        private Language language;
        private List<Type> visited = new List<Type>();
        private Queue<Type> toVisit = new Queue<Type>();

        public void Visit(IEnumerable<Type> types, Language language)
        {
            this.language = language;

            foreach (var type in types)
            {
                this.RegisterType(type);
            }

            while (this.toVisit.Count > 0)
            {
                var type = this.toVisit.Dequeue();
                if (!this.visited.Contains(type))
                {
                    VisitType(type);
                }
            }
        }

        private List<string> IgnoreLanguages(MemberInfo member)
        {
            var attributeData = member
                .GetCustomAttributes<IgnoreForLanguageGeneratorAttribute>()
                .ToList();

            var attributeNames = attributeData
                .Select(a => a.LanguageId)
                .ToList();

            return attributeNames;
        }

        private void VisitType(Type type)
        {
            this.visited.Add(type);

           
            var nsArgs = new NameSpaceEventArgs
            {
                NameSpaceName = NameSpace(type),
                Comment = Comment(type)
            };

            var args = new TypeEventArgs() 
            { 
                TypeName = TypeName(type),
                NameSpaceName = NameSpace(type)
            };

            if (NameSpaceVisiting != null)
            {
                NameSpaceVisiting(this, nsArgs);
            }

            if (TypeVisiting != null)
            {
                TypeVisiting(this, args); 
            }

            foreach (var property in type.GetProperties())
            {
                VisitProperty(property);
            }

            foreach (var field in type.GetFields())
            {
                VisitField(field);
            } 
            
            foreach (var member in type.GetMethods())
            {
                VisitMethod(member);
            }

            if (TypeVisited != null) 
            {
                TypeVisited(this, args); 
            }

            if (NameSpaceVisited != null)
            {
                NameSpaceVisited(this, nsArgs);
            }
        }

        private string Comment(Type type)
        {
            return string.Format("{1}.{0}", type.Name, type.Namespace);
        }

        private string NameSpace(Type type)
        {
            return this.language.GetTypeInfo(type).NameSpaceName;
        }
        private string FullName(Type type)
        {
            return this.language.GetTypeInfo(type).FullName;
        }
        private void VisitField(FieldInfo member)
        {
            var args = GetMemberEventArgs(member, member.FieldType);

            if (MemberVisiting != null) { MemberVisiting(this, args); }

            this.RegisterType(member.FieldType);


            if (MemberVisited != null) { MemberVisited(this, args); }
        }

        private bool ShouldVisit(Type type)
        {
            return TypeExtensions.IsExportableType(type);
        }

        private MemberEventArgs GetMemberEventArgs(MemberInfo member, Type type)
        {
            var typeInfo = this.language.GetTypeInfo(type); 
            var args = new MemberEventArgs()
            {
                MemberName = member.Name,
                MemberTypeName = typeInfo.Name,
                MemberTypeFullName = typeInfo.FullName,
                MemberTypeNameSpaceName = typeInfo.NameSpaceName,
                IgnoredByGenerators = this.IgnoreLanguages(member)
            };

            return args; 
        }

        private void VisitProperty(PropertyInfo member)
        {
            var args = GetMemberEventArgs(member, member.PropertyType);

            if (MemberVisiting != null) { MemberVisiting(this, args); }
           
            this.RegisterType(member.PropertyType);

            if (MemberVisited != null) { MemberVisited(this, args); }
        }

        private void RegisterType(Type type)
        {
            Type collectionOf;
            if (type.IsGenericCollectionType(out collectionOf))
            {
                RegisterType(collectionOf);
                return;
            }

            if (!ShouldVisit(type))
            {
                return;
            }
            
            AddIfMissing(type);

            foreach (var i in type.GetInterfaces())
            {
                RegisterType(i);
            }

            if (type.BaseType != null)
            {
                RegisterType(type.BaseType);
            }
        }

        private void AddIfMissing(Type type)
        {
            if (!this.visited.Contains(type) && !this.toVisit.Contains(type))
            {
                this.toVisit.Enqueue(type);
            }
        }

        private void VisitMethod(MethodInfo method)
        {
            var args = new MethodEventArgs() 
            {
                MethodName = method.Name 
            };

            if (MethodVisiting != null) { MethodVisiting(this, args); }
            if (MethodVisited != null) { MethodVisited(this, args); }
        }

        private string TypeName(Type type)
        {
            var name = this.language.GetTypeInfo(type);
            return name.Name;
        }
    }
}
