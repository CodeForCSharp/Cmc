﻿using System.Collections.Generic;
using System.Linq;
using bCC.Core;
using bCC.Expression;
using JetBrains.Annotations;
using static System.StringComparison;

#pragma warning disable 659

namespace bCC
{
	public class Declaration : Statement.Statement
	{
		[NotNull] public readonly string Name;
		public readonly Modifier Modifier;

		public Declaration(
			MetaData metaData,
			[NotNull] string name,
			Modifier modifier) : base(metaData)
		{
			Name = name;
			Modifier = modifier;
		}
	}

	public sealed class VariableDeclaration : Declaration
	{
		[NotNull] public readonly Expression.Expression Expression;
		public readonly bool Mutability;
		public Type Type;

		public VariableDeclaration(
			MetaData metaData,
			[NotNull] string name,
			Modifier modifier,
			[CanBeNull] Expression.Expression expression = null,
			bool isMutable = false,
			[CanBeNull] Type type = null) :
			base(metaData, name, modifier)
		{
			Expression = expression ?? new NullExpression(MetaData);
			Type = type;
			Mutability = isMutable;
		}

		public override void SurroundWith(Environment environment)
		{
			base.SurroundWith(environment);
			Expression.SurroundWith(Env);
			var exprType = Expression.GetExpressionType();
			// FEATURE #8
			Type = Type ?? exprType;
			// FEATURE #30
			Type.SurroundWith(Env);
			if (Type is UnknownType unknownType) Type = unknownType.Resolve();
			// FEATURE #11
			if (!string.Equals(exprType.ToString(), PrimaryType.NullType, Ordinal) && !Equals(Type, exprType))
				// FEATURE #9
				Errors.Add($"{MetaData.GetErrorHeader()}type mismatch, expected: {Type}, actual: {exprType}");
		}

		public override bool Equals(object obj) => obj is Declaration declaration && declaration.Name == Name;

		public override IEnumerable<string> Dump() => new[]
			{
				$"variable declaration [{Name}]:\n",
				"  type:\n"
			}
			.Concat(Type.Dump().Select(MapFunc2))
			.Concat(new[] {"  initialize expression:\n"})
			.Concat(Expression.Dump().Select(MapFunc2));
	}

	/// <summary>
	///   type aliases
	///   FEATURE #31
	/// </summary>
	public class TypeDeclaration : Declaration
	{
		public readonly Type Type;

		public TypeDeclaration(
			MetaData metaData,
			[NotNull] string name,
			Modifier modifier,
			[NotNull] Type type)
			: base(metaData, name, modifier) => Type = type;
	}

	public class StructDeclaration : Declaration
	{
		public readonly IList<VariableDeclaration> FieldList;
		public readonly Type Type;

		public StructDeclaration(
			MetaData metaData,
			[NotNull] string name,
			Modifier modifier,
			[NotNull] IList<VariableDeclaration> fieldList) :
			base(metaData, name, modifier)
		{
			FieldList = fieldList;
			Type = new SecondaryType(metaData, name, this);
		}

		public override void SurroundWith(Environment environment)
		{
			base.SurroundWith(environment);
			var internalEnv = new Environment(Env);
			foreach (var variableDeclaration in FieldList)
				variableDeclaration.SurroundWith(internalEnv);
		}
	}

	/// <summary>
	///   Probably useless
	/// </summary>
	public class Macro : Declaration
	{
		[NotNull] public string Content;

		public Macro(
			MetaData metaData,
			[NotNull] string name,
			Modifier modifier,
			[NotNull] string content) :
			base(metaData, name, modifier) =>
			Content = content;

		public override IEnumerable<string> Dump() => new[] {"macro(this shouldn't appear)\n"};
	}
}