﻿using Cmc;
using Cmc.Core;
using Cmc.Expr;
using Cmc.Stmt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmcTest
{
	[TestClass]
	public class ReturnTests
	{
		private const string Var = "var";

		public static LambdaExpression Block2() => new LambdaExpression(MetaData.Empty,
			new StatementList(MetaData.Empty,
				new IfStatement(MetaData.Empty,
					new VariableExpression(MetaData.Empty, Var),
					new StatementList(MetaData.Empty),
					new StatementList(MetaData.Empty))));

		public static LambdaExpression Block1() => new LambdaExpression(MetaData.Empty,
			new StatementList(MetaData.Empty,
				new IfStatement(MetaData.Empty,
					new VariableExpression(MetaData.Empty, Var),
					new StatementList(MetaData.Empty),
					new StatementList(MetaData.Empty))));

		[TestInitialize]
		public void Init() => Errors.ErrList.Clear();

		[TestMethod]
		public void ReturnTest1()
		{
			var block = Block1();
			block.SurroundWith(Environment.SolarSystem);
			block.PrintDumpInfo();
			Errors.PrintErrorInfo();
			Assert.IsTrue(0 != Errors.ErrList.Count);
		}

		[TestMethod]
		public void ReturnTest2()
		{
			var block = Block2();
			block.SurroundWith(Environment.SolarSystem);
			block.PrintDumpInfo();
			Assert.IsTrue(0 == Errors.ErrList.Count);
		}
	}
}