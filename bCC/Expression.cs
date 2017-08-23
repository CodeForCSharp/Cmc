﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

#pragma warning disable 659

namespace bCC
{
	public abstract class Expression : Ast
	{
		[NotNull]
		public abstract Type GetExpressionType();

		protected Expression(MetaData metaData) : base(metaData)
		{
		}
	}

	public abstract class AtomicExpression : Expression
	{
		protected AtomicExpression(MetaData metaData) : base(metaData)
		{
		}
	}

	public sealed class NullExpression : Expression
	{
		public const string NullType = "nulltype";

		public NullExpression(MetaData metaData) : base(metaData)
		{
		}

		public override Type GetExpressionType() => new SecondaryType(MetaData, NullType);

		public override IEnumerable<string> Dump() => new[] {"null expression\n"};
	}

	public class LiteralExpression : AtomicExpression
	{
		[NotNull] public readonly Type Type;
		protected LiteralExpression(MetaData metaData, [NotNull] Type type) : base(metaData) => Type = type;
		public override Type GetExpressionType() => Type;
	}

	public class IntLiteralExpression : LiteralExpression
	{
		[NotNull] public readonly string Value;

		public IntLiteralExpression(MetaData metaData, [NotNull] string value, bool isSigned, int length = 32)
			: base(metaData, new SecondaryType(metaData, (isSigned ? "i" : "u") + length)) => Value = value;

		public override IEnumerable<string> Dump() =>
			new[]
					{"literal expression:\n"}
				.Concat(Type.Dump().Select(MapFunc))
				.Concat(new[]
				{
					"  value [" + Value + "]\n"
				});
	}

	/// <summary>
	/// A function is a variable with the type of lambda
	/// This is the class for anonymous lambda
	/// </summary>
	public class Lambda : AtomicExpression
	{
		[NotNull] public readonly StatementList Body;
		private Type _type;
		private Environment _env;

		public override Environment Env
		{
			get => _env;
			[NotNull]
			set
			{
				_env = value;
				Body.Env = Env;
				// FEATURE #12
				_type = Body.Statements.Last() is ReturnStatement ret
					? ret.Expression.GetExpressionType()
					: new SecondaryType(MetaData, "void");
			}
		}

		public override Type GetExpressionType() => _type;

		public Lambda(MetaData metaData, [NotNull] StatementList body) : base(metaData) => Body = body;
	}

	public class VariableExpression : AtomicExpression
	{
		public override Environment Env
		{
			get => _env;
			set
			{
				_env = value;
				var declaration = Env.FindDeclarationByName(Name);
				if (declaration is VariableDeclaration variableDeclaration) _type = variableDeclaration.Type;
				else Errors.Add(MetaData.GetErrorHeader() + "Wtf");
			}
		}

		[NotNull] public readonly string Name;
		private Type _type;
		private Environment _env;

		public override Type GetExpressionType() => _type ?? throw new CompilerException();

		public VariableExpression(MetaData metaData, [NotNull] string name) : base(metaData) => Name = name;

		public override IEnumerable<string> Dump() => new[]
			{
				"variable expression:\n",
				"  name: " + Name + "\n",
				"  type:\n"
			}
			.Concat(_type.Dump().Select(MapFunc).Select(MapFunc));
	}

	public class FunctionCallExpression : AtomicExpression
	{
		public override Environment Env
		{
			[NotNull] get => _env;
			set
			{
				_env = value;
				Receiver.Env = Env;
				foreach (var expression in ParameterList) expression.Env = Env;
				// TODO check parameter type
				var hisType = Receiver.GetExpressionType();
				if (hisType is LambdaType lambdaType) _type = lambdaType.RetType;
				else
					Errors.Add(MetaData.GetErrorHeader() + "the function call receiver shoule be a function, not " + hisType + ".");
				foreach (var expression in ParameterList) expression.Env = Env;
			}
		}

		[NotNull] public readonly Expression Receiver;
		[NotNull] public readonly IList<Expression> ParameterList;
		private Type _type;
		private Environment _env;

		public FunctionCallExpression(MetaData metaData, [NotNull] Expression receiver,
			[NotNull] IList<Expression> parameterList) :
			base(metaData)
		{
			ParameterList = parameterList;
			Receiver = receiver;
		}

		public override Type GetExpressionType() => _type ?? throw new CompilerException();
	}
}