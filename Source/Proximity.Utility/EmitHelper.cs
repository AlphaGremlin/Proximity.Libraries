/****************************************\
 EmitHelper.cs
 Created: 8-06-2009
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Provides services for rapidly emitting dynamic code
	/// </summary>
	public class EmitHelper
	{	//****************************************
		private ILGenerator _Generator;
		
		private DynamicMethod _DestMethod;
		
		private Dictionary<string, Label> _Labels = new Dictionary<string, Label>();
		private Dictionary<string, LocalBuilder> _Locals = new Dictionary<string, LocalBuilder>();
		//****************************************
		
		/// <summary>
		/// Creates a new EmitHelper around a pre-created Dynamic Method
		/// </summary>
		/// <param name="destMethod">The Dynamic Method to wrap</param>
		public EmitHelper(DynamicMethod destMethod)
		{
			_DestMethod = destMethod;
			
			_Generator = _DestMethod.GetILGenerator();
		}
		
		//****************************************
		
		/// <summary>
		/// Generates an EmitHelper targeting a dynamic method with no return value
		/// </summary>
		/// <param name="name">The name of the dynamic method</param>
		/// <param name="owner">The type that owns the method. Null for no owner</param>
		/// <param name="arguments">The type arguments of the method</param>
		/// <returns>An EmitHelper for generating the method body</returns>
		public static EmitHelper FromAction(string name, Type owner, params Type[] arguments)
		{
			if (owner == null)
				return new EmitHelper(new DynamicMethod(name, null, arguments));
			
			return new EmitHelper(new DynamicMethod(name, null, arguments, owner, true));
		}
		
		/// <summary>
		/// Generates an EmitHelper targeting a dynamic method with a return value
		/// </summary>
		/// <param name="name">The name of the dynamic method</param>
		/// <param name="owner">The type that owns the method. Null for no owner</param>
		/// <param name="result">The type of the result of the method</param>
		/// <param name="arguments">The type arguments of the method</param>
		/// <returns>An EmitHelper for generating the method body</returns>
		public static EmitHelper FromFunction(string name, Type owner, Type result, params Type[] arguments)
		{
			if (owner == null)
				return new EmitHelper(new DynamicMethod(name, result, arguments));
			
			return new EmitHelper(new DynamicMethod(name, result, arguments, owner, true));
		}
		
		/// <summary>
		/// Finds the method matching the given parameters, searching any interfaces implemented by the type as well
		/// </summary>
		/// <param name="targetType">The type being targeted</param>
		/// <param name="methodName">The name of the method</param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static MethodInfo GetMethod(Type targetType, string methodName, params Type[] parameters)
		{
			MethodInfo MyMethod = targetType.GetMethod(methodName, parameters);
			
			if (MyMethod == null && targetType.IsInterface)
			{
				foreach(Type MyType in targetType.GetInterfaces())
				{
					MyMethod = GetMethod(MyType, methodName, parameters);
				
					if (MyMethod != null)
						return MyMethod;
				}
			}
			
			return MyMethod;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetType"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public static FieldInfo GetField(Type targetType, string fieldName)
		{
			FieldInfo MyField = targetType.GetField(fieldName);
			
			if (MyField == null && targetType.IsInterface)
			{
				foreach(Type MyType in targetType.GetInterfaces())
				{
					MyField = GetField(MyType, fieldName);
				
					if (MyField != null)
						return MyField;
				}		
			}
			
			return MyField;
		}
		
		//****************************************
		// Exception Handling
		
		/// <summary>
		/// Begins a Try Block
		/// </summary>
		/// <param name="labelName">The name of the Label to mark the end of the Try Block</param>
		/// <returns>Self</returns>
		public EmitHelper BeginTry(string labelName)
		{
			if (_Labels.ContainsKey(labelName))
				throw new ArgumentException("Label Name already exists");
			
			_Labels.Add(labelName, _Generator.BeginExceptionBlock());
			
			return this;
		}
		
		/// <summary>
		/// Begins a Try Block
		/// </summary>
		/// <returns>A Label to mark the end of the Try Block</returns>
		public Label BeginTry()
		{
			return _Generator.BeginExceptionBlock();
		}
		
		/// <summary>
		/// Begins a Try Block
		/// </summary>
		/// <param name="label">A variable to receive the Label Builder</param>
		/// <returns>Self</returns>
		public EmitHelper BeginTry(out Label label)
		{
			label = _Generator.BeginExceptionBlock();
			
			return this;
		}
		
		/// <summary>
		/// Begins a Catch Block
		/// </summary>
		/// <param name="exceptionType">The Type of exception to Catch</param>
		/// <returns>Self</returns>
		public EmitHelper BeginCatch(Type exceptionType)
		{
			_Generator.BeginCatchBlock(exceptionType);
			
			return this;
		}
		
		/// <summary>
		/// Begins a Filter Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper BeginFilter
		{
			get { _Generator.BeginExceptFilterBlock(); return this; }
		}
		
		/// <summary>
		/// Begins a Fault Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper BeginFault
		{
			get { _Generator.BeginFaultBlock(); return this; }
		}
		
		/// <summary>
		/// Begins a Finally Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper BeginFinally
		{
			get { _Generator.BeginFinallyBlock(); return this; }
		}
		
		/// <summary>
		/// Ends a Try Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper EndTry
		{
			get { _Generator.EndExceptionBlock(); return this; }
		}
		
		/// <summary>
		/// Ends a Filter Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper EndFilter
		{
			get { _Generator.Emit(OpCodes.Endfilter); return this; }
		}
		
		/// <summary>
		/// Ends a Finally Block
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper EndFinally
		{
			get { _Generator.Emit(OpCodes.Endfinally); return this; }
		}
		
		/// <summary>
		/// Throws the Exception on the Stack
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Throw
		{
			get { _Generator.Emit(OpCodes.Throw); return this; }
		}
		
		/// <summary>
		/// Throws an Exception where the Type has a default constructor
		/// </summary>
		/// <param name="exceptionType">The Type of Exception to throw</param>
		/// <returns>Self</returns>
		public EmitHelper ThrowException(Type exceptionType)
		{
			_Generator.ThrowException(exceptionType);
			
			return this;
		}
		
		/// <summary>
		/// Rethrows the Exception on the Stack
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Rethrow
		{
			get { _Generator.Emit(OpCodes.Rethrow); return this; }
		}
		
		//****************************************
		// Declarations and Labelling
		
		/// <summary>
		/// Declares a named Local Variable
		/// </summary>
		/// <param name="localName">The internal name of the Local Variable</param>
		/// <param name="localType">The Type of the Local Variable</param>
		/// <returns>Self</returns>
		public EmitHelper DeclareLocal(string localName, Type localType)
		{
			_Locals.Add(localName, _Generator.DeclareLocal(localType));
			
			return this;
		}
		
		/// <summary>
		/// Declares a Local Variable
		/// </summary>
		/// <param name="localType">The Type of the Local Variable</param>
		/// <returns>A Local Variable Builder</returns>
		public LocalBuilder DeclareLocal(Type localType)
		{
			return _Generator.DeclareLocal(localType);
		}
		
		/// <summary>
		/// Declares a Local Variable
		/// </summary>
		/// <param name="localType">The Type of the Local Variable</param>
		/// <param name="local">A variable to receive the Local Variable Builder</param>
		/// <returns>Self</returns>
		public EmitHelper DeclareLocal(Type localType, out LocalBuilder local)
		{
			local = _Generator.DeclareLocal(localType);
			
			return this;
		}
		
		/// <summary>
		/// Declares a named Local Variable if it doesn't exist
		/// </summary>
		/// <param name="localName">The internal name of the Local Variable</param>
		/// <param name="localType">The Type of the Local Variable</param>
		/// <returns>Self</returns>
		/// <remarks>Checks that the local is of the correct type if defined</remarks>
		public EmitHelper EnsureLocal(string localName, Type localType)
		{
			LocalBuilder MyBuilder;
			
			if (_Locals.TryGetValue(localName, out MyBuilder))
			{
				if (MyBuilder.LocalType != localType)
					throw new ArrayTypeMismatchException("Local is already declared with a different type");
			}
			else
			{
				_Locals.Add(localName, _Generator.DeclareLocal(localType));
			}
			
			return this;
		}
		
		/// <summary>
		/// Returns whether a named Local Variable is defined
		/// </summary>
		/// <param name="localName">The internal name of the Local Variable</param>
		/// <returns>True if it exists, otherwise false</returns>
		/// <seealso cref="DeclareLocal(string,Type)" />
		public bool HasLocal(string localName)
		{
			return _Locals.ContainsKey(localName);
		}
		
		/// <summary>
		/// Declares a named Label
		/// </summary>
		/// <param name="labelName">The internal name of the Label</param>
		/// <returns>Self</returns>
		public EmitHelper DeclareLabel(string labelName)
		{
			_Labels.Add(labelName, _Generator.DefineLabel());
			
			return this;
		}
		
		/// <summary>
		/// Declares a Label
		/// </summary>
		/// <returns>A new Label</returns>
		public Label DeclareLabel()
		{
			return _Generator.DefineLabel();
		}
		
		/// <summary>
		/// Declares a Label
		/// </summary>
		/// <param name="label">A variable to receive the Label Builder</param>
		/// <returns>Self</returns>
		public EmitHelper DeclareLabel(out Label label)
		{
			label = _Generator.DefineLabel();
			
			return this;
		}
		
		/// <summary>
		/// Marks a named Label at the current instruction
		/// </summary>
		/// <param name="labelName">The internal name of the Label</param>
		/// <returns>Self</returns>
		public EmitHelper MarkLabel(string labelName)
		{
			_Generator.MarkLabel(GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Marks a Label at the current instruction
		/// </summary>
		/// <param name="label">The Label to mark</param>
		/// <returns>Self</returns>
		public EmitHelper MarkLabel(Label label)
		{
			_Generator.MarkLabel(label);
			
			return this;
		}
		
		/// <summary>
		/// Begins a variable scope
		/// </summary>
		/// <returns>Self</returns>
		public EmitHelper BeginScope()
		{
			_Generator.BeginScope();
			
			return this;
		}
		
		/// <summary>
		/// Ends a Variable scope
		/// </summary>
		/// <returns></returns>
		public EmitHelper EndScope()
		{
			_Generator.EndScope();
			
			return this;
		}
		
		//****************************************
		// Mathematics
		
		/// <summary>
		/// Emits an addition instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Add
		{
			get { _Generator.Emit(OpCodes.Add); return this; }
		}
		
		/// <summary>
		/// Emits an addition with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper AddOvf
		{
			get { _Generator.Emit(OpCodes.Add_Ovf); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned addition with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper AddOvfUn
		{
			get { _Generator.Emit(OpCodes.Add_Ovf_Un); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise And instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper And
		{
			get { _Generator.Emit(OpCodes.And); return this; }
		}
		
		/// <summary>
		/// Emits a division instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Div
		{
			get { _Generator.Emit(OpCodes.Div); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned divison instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper DivUn
		{
			get { _Generator.Emit(OpCodes.Div_Un); return this; }
		}
		
		/// <summary>
		/// Emits a multiplication instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Mul
		{
			get { _Generator.Emit(OpCodes.Mul); return this; }
		}
		
		/// <summary>
		/// Emits a multiplication with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper MulOvf
		{
			get { _Generator.Emit(OpCodes.Mul_Ovf); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned multiplication with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper MulOvfUn
		{
			get { _Generator.Emit(OpCodes.Mul_Ovf_Un); return this; }
		}
		
		/// <summary>
		/// Emits a negation instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Neg
		{
			get { _Generator.Emit(OpCodes.Neg); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise Not instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Not
		{
			get { _Generator.Emit(OpCodes.Not); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise Or instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Or
		{
			get { _Generator.Emit(OpCodes.Or); return this; }
		}

		/// <summary>
		/// Emits a division remainder instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Rem
		{
			get { _Generator.Emit(OpCodes.Rem); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned division remainder instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper RemUn
		{
			get { _Generator.Emit(OpCodes.Rem_Un); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise shift-left instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Shl
		{
			get { _Generator.Emit(OpCodes.Shl); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise shift-right instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Shr
		{
			get { _Generator.Emit(OpCodes.Shr); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned bitwise shift-right instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper ShrUn
		{
			get { _Generator.Emit(OpCodes.Shr_Un); return this; }
		}
		
		/// <summary>
		/// Emits a subtraction instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Sub
		{
			get { _Generator.Emit(OpCodes.Sub); return this; }
		}
		
		/// <summary>
		/// Emits a subtraction with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper SubOvf
		{
			get { _Generator.Emit(OpCodes.Sub_Ovf); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned subtraction with overflow instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper SubOvfUn
		{
			get { _Generator.Emit(OpCodes.Sub_Ovf_Un); return this; }
		}
		
		/// <summary>
		/// Emits a bitwise XOR instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Xor
		{
			get { _Generator.Emit(OpCodes.Xor); return this; }
		}
				
		//****************************************
		// Comparisons
		
		/// <summary>
		/// Emits a Compare if Equal instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Ceq
		{
			get { _Generator.Emit(OpCodes.Ceq); return this; }
		}
		
		/// <summary>
		/// Emits a Compare if Greater Than instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Cgt
		{
			get { _Generator.Emit(OpCodes.Cgt); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned Compare if Greater Than instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper CgtUn
		{
			get { _Generator.Emit(OpCodes.Cgt_Un); return this; }
		}
		
		/// <summary>
		/// Emits a check if a floating point number is finite
		/// </summary>
		public EmitHelper CkFinite
		{
			get { _Generator.Emit(OpCodes.Ckfinite); return this; }
		}
		
		/// <summary>
		/// Emits a Compare if Less Than instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Clt
		{
			get { _Generator.Emit(OpCodes.Clt); return this; }
		}
		
		/// <summary>
		/// Emits an unsigned Compare if Less Than instruction
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper CltUn
		{
			get { _Generator.Emit(OpCodes.Clt_Un); return this; }
		}
		
		//****************************************
		// Branching
		
		/// <summary>
		/// Emits a Branch if Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Beq(string labelName)
		{
			_Generator.Emit(OpCodes.Beq, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Beq(Label label)
		{
			_Generator.Emit(OpCodes.Beq, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BeqS(string labelName)
		{
			_Generator.Emit(OpCodes.Beq_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BeqS(Label label)
		{
			_Generator.Emit(OpCodes.Beq_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater or Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Bge(string labelName)
		{
			_Generator.Emit(OpCodes.Bge, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater or Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Bge(Label label)
		{
			_Generator.Emit(OpCodes.Bge, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater or Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeS(string labelName)
		{
			_Generator.Emit(OpCodes.Bge_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater or Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeS(Label label)
		{
			_Generator.Emit(OpCodes.Bge_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater or Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeUn(string labelName)
		{
			_Generator.Emit(OpCodes.Bge_Un, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater or Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeUn(Label label)
		{
			_Generator.Emit(OpCodes.Bge_Un, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater or Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeUnS(string labelName)
		{
			_Generator.Emit(OpCodes.Bge_Un_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater or Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgeUnS(Label label)
		{
			_Generator.Emit(OpCodes.Bge_Un_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater Than instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Bgt(string labelName)
		{
			_Generator.Emit(OpCodes.Bgt, GetLabel(labelName));
			
			return this;
		}
		
			/// <summary>
		/// Emits a Branch if Greater Than instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Bgt(Label label)
		{
			_Generator.Emit(OpCodes.Bgt, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater Than (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtS(string labelName)
		{
			_Generator.Emit(OpCodes.Bgt_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Greater Than (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtS(Label label)
		{
			_Generator.Emit(OpCodes.Bgt_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater Than instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtUn(string labelName)
		{
			_Generator.Emit(OpCodes.Bgt_Un, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater Than instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtUn(Label label)
		{
			_Generator.Emit(OpCodes.Bgt_Un, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater Than (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtUnS(string labelName)
		{
			_Generator.Emit(OpCodes.Bgt_Un_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Greater Than (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BgtUnS(Label label)
		{
			_Generator.Emit(OpCodes.Bgt_Un_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less or Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Ble(string labelName)
		{
			_Generator.Emit(OpCodes.Ble, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less or Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Ble(Label label)
		{
			_Generator.Emit(OpCodes.Ble, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less or Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleS(string labelName)
		{
			_Generator.Emit(OpCodes.Ble_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less or Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleS(Label label)
		{
			_Generator.Emit(OpCodes.Ble_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less or Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleUn(string labelName)
		{
			_Generator.Emit(OpCodes.Ble_Un, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less or Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleUn(Label label)
		{
			_Generator.Emit(OpCodes.Ble_Un, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less or Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleUnS(string labelName)
		{
			_Generator.Emit(OpCodes.Ble_Un_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less or Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BleUnS(Label label)
		{
			_Generator.Emit(OpCodes.Ble_Un_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less Than instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Blt(string labelName)
		{
			_Generator.Emit(OpCodes.Blt, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less Than instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Blt(Label label)
		{
			_Generator.Emit(OpCodes.Blt, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less Than (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltS(string labelName)
		{
			_Generator.Emit(OpCodes.Blt_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if Less Than (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltS(Label label)
		{
			_Generator.Emit(OpCodes.Blt_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less Than instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltUn(string labelName)
		{
			_Generator.Emit(OpCodes.Blt_Un, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less Than instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltUn(Label label)
		{
			_Generator.Emit(OpCodes.Blt_Un, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less Than (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltUnS(string labelName)
		{
			_Generator.Emit(OpCodes.Blt_Un_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Less Than (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BltUnS(Label label)
		{
			_Generator.Emit(OpCodes.Blt_Un_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Not Equal instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BneUn(string labelName)
		{
			_Generator.Emit(OpCodes.Bne_Un, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Not Equal instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BneUn(Label label)
		{
			_Generator.Emit(OpCodes.Bne_Un, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Not Equal (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BneUnS(string labelName)
		{
			_Generator.Emit(OpCodes.Bne_Un_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an unsigned Branch if Not Equal (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BneUnS(Label label)
		{
			_Generator.Emit(OpCodes.Bne_Un_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Br(string labelName)
		{
			_Generator.Emit(OpCodes.Br, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Br(Label label)
		{
			_Generator.Emit(OpCodes.Br, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrS(string labelName)
		{
			_Generator.Emit(OpCodes.Br_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrS(Label label)
		{
			_Generator.Emit(OpCodes.Br_S, label);
			
			return this;
		}
	
		/// <summary>
		/// Emits a Branch if False instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrFalse(string labelName)
		{
			_Generator.Emit(OpCodes.Brfalse, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if False instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrFalse(Label label)
		{
			_Generator.Emit(OpCodes.Brfalse, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if False (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrFalseS(string labelName)
		{
			_Generator.Emit(OpCodes.Brfalse_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if False (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrFalseS(Label label)
		{
			_Generator.Emit(OpCodes.Brfalse_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if True instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrTrue(string labelName)
		{
			_Generator.Emit(OpCodes.Brtrue, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if True instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrTrue(Label label)
		{
			_Generator.Emit(OpCodes.Brtrue, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if True (short) instruction
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrTrueS(string labelName)
		{
			_Generator.Emit(OpCodes.Brtrue_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a Branch if True (short) instruction
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper BrTrueS(Label label)
		{
			_Generator.Emit(OpCodes.Brtrue_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a jump instruction to another method
		/// </summary>
		/// <param name="methodInfo">The targeted method</param>
		/// <returns>Self</returns>
		public EmitHelper Jmp(MethodInfo methodInfo)
		{
			_Generator.Emit(OpCodes.Jmp, methodInfo);
			return this;
		}
		
		/// <summary>
		/// Emits a leave instruction to exit a protected region of code
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Leave(string labelName)
		{
			_Generator.Emit(OpCodes.Leave, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a leave instruction to exit a protected region of code
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper Leave(Label label)
		{
			_Generator.Emit(OpCodes.Leave, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a leave (short) instruction to exit a protected region of code
		/// </summary>
		/// <param name="labelName">The named Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper LeaveS(string labelName)
		{
			_Generator.Emit(OpCodes.Leave_S, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits a leave (short) instruction to exit a protected region of code
		/// </summary>
		/// <param name="label">The Label to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper LeaveS(Label label)
		{
			_Generator.Emit(OpCodes.Leave_S, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits a switch instruction to jump based on a value
		/// </summary>
		/// <param name="labelNames">An array of named Labels to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper @Switch(params string[] labelNames)
		{	//****************************************
			Label[] ResultLabels;
			//****************************************
			
			ResultLabels = Array.ConvertAll<string, Label>(labelNames, delegate(string input) { return this.GetLabel(input); });
			
			_Generator.Emit(OpCodes.Switch, ResultLabels);
			
			return this;
		}
		
		/// <summary>
		/// Emits a switch instruction to jump based on a value
		/// </summary>
		/// <param name="labels">An array of Labels to branch to</param>
		/// <returns>Self</returns>
		public EmitHelper @Switch(params Label[] labels)
		{
			_Generator.Emit(OpCodes.Switch, labels);
			
			return this;
		}
		
		//****************************************
		// Calling
		
		/// <summary>
		/// Emits an instruction to call a method
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <returns>Self</returns>
		public EmitHelper Call(MethodInfo targetMethod)
		{
			_Generator.Emit(OpCodes.Call, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method with variable arguments
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <param name="optionalParameterTypes">The types of each variable argument</param>
		/// <returns>Self</returns>
		public EmitHelper Call(MethodInfo targetMethod, Type[] optionalParameterTypes)
		{
			_Generator.EmitCall(OpCodes.Call, targetMethod, optionalParameterTypes);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper Call(Type targetType, string targetName, params Type[] optionalParameterTypes)
		{
			MethodInfo targetMethod = targetType.GetMethod(targetName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, optionalParameterTypes, null);

			if (targetMethod == null)
				throw new ArgumentException(string.Format("Method '{0}' does not exist on Type '{1}'", targetName, targetType.FullName));
			
			_Generator.Emit(OpCodes.Call, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="flags">The appropriate binding flags for the method</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper Call(Type targetType, string targetName, BindingFlags flags, params Type[] optionalParameterTypes)
		{
			MethodInfo TargetMethod;
			
			if (optionalParameterTypes == null)
				TargetMethod = targetType.GetMethod(targetName, flags);
			else
				TargetMethod = targetType.GetMethod(targetName, flags, null, optionalParameterTypes, null);

			if (TargetMethod == null)
				throw new ArgumentException(string.Format("Method '{0}' does not exist on Type '{1}'", targetName, targetType.FullName));
			
			_Generator.Emit(OpCodes.Call, TargetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to indirectly call a method
		/// </summary>
		/// <param name="callingConvention">The calling convention to use</param>
		/// <param name="returnType">The return type from the method</param>
		/// <param name="parameterTypes">The type of each argument to pass</param>
		/// <returns>Self</returns>
		public EmitHelper CallI(CallingConvention callingConvention, Type returnType, Type[] parameterTypes)
		{
			_Generator.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes);
			
			return this;
		}		
		
		/// <summary>
		/// Emits an instruction to indirectly call a varargs method
		/// </summary>
		/// <param name="callingConvention">The calling convention to use</param>
		/// <param name="returnType">The return type from the method</param>
		/// <param name="parameterTypes">The type of each argument to pass</param>
		/// <param name="optionalParameterTypes">The type of each variable argument being passed</param>
		/// <returns>Self</returns>
		public EmitHelper CallI(CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
		{
			_Generator.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes, optionalParameterTypes);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a virtual method
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <returns>Self</returns>
		public EmitHelper CallVirt(MethodInfo targetMethod)
		{
			_Generator.Emit(OpCodes.Callvirt, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a virtual method with variable arguments
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <param name="optionalParameterTypes">The types of each variable argument</param>
		/// <returns>Self</returns>
		public EmitHelper CallVirt(MethodInfo targetMethod, Type[] optionalParameterTypes)
		{
			_Generator.EmitCall(OpCodes.Callvirt, targetMethod, optionalParameterTypes);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a virtual method
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper CallVirt(Type targetType, string targetName, params Type[] optionalParameterTypes)
		{
			MethodInfo targetMethod = GetMethod(targetType, targetName, optionalParameterTypes);

			if (targetMethod == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(OpCodes.Callvirt, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a virtual method
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="flags">The appropriate binding flags for the method</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper CallVirt(Type targetType, string targetName, BindingFlags flags, params Type[] optionalParameterTypes)
		{
			MethodInfo TargetMethod;
			
			if (optionalParameterTypes == null)
				TargetMethod = targetType.GetMethod(targetName, flags);
			else
				TargetMethod = targetType.GetMethod(targetName, flags, Type.DefaultBinder, optionalParameterTypes, null);

			if (TargetMethod == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(OpCodes.Callvirt, TargetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method, detecting whether to use Call or Callvirt
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <returns>Self</returns>
		public EmitHelper CallSmart(MethodInfo targetMethod)
		{
			_Generator.Emit(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method with variable arguments, detecting whether to use Call or Callvirt
		/// </summary>
		/// <param name="targetMethod">The target method</param>
		/// <param name="optionalParameterTypes">The types of each variable argument</param>
		/// <returns>Self</returns>
		public EmitHelper CallSmart(MethodInfo targetMethod, Type[] optionalParameterTypes)
		{
			_Generator.EmitCall(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod, optionalParameterTypes);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method, detecting whether to use Call or Callvirt
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper CallSmart(Type targetType, string targetName, params Type[] optionalParameterTypes)
		{
			MethodInfo TargetMethod = GetMethod(targetType, targetName, optionalParameterTypes);

			if (TargetMethod == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(TargetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, TargetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to call a method, detecting whether to use Call or Callvirt
		/// </summary>
		/// <param name="targetType">The type owning the method to call</param>
		/// <param name="targetName">The name of the method to call</param>
		/// <param name="flags">The appropriate binding flags for the method</param>
		/// <param name="optionalParameterTypes">The types of each argument on the method</param>
		/// <returns>Self</returns>
		public EmitHelper CallSmart(Type targetType, string targetName, BindingFlags flags, params Type[] optionalParameterTypes)
		{
			MethodInfo TargetMethod;
			
			if (optionalParameterTypes == null)
				TargetMethod = targetType.GetMethod(targetName, flags);
			else
				TargetMethod = targetType.GetMethod(targetName, flags, Type.DefaultBinder, optionalParameterTypes, null);

			if (TargetMethod == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(TargetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, TargetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits a constrain instruction
		/// </summary>
		/// <param name="targetType">The target to constrain to</param>
		/// <returns>Self</returns>
		/// <remarks>Should be followed by a Call/CallI/CallVirt</remarks>
		public EmitHelper Constrained(Type targetType)
		{
			_Generator.Emit(OpCodes.Constrained, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to return from the method
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Ret
		{
			get { _Generator.Emit(OpCodes.Ret); return this;}
		}
		
		/// <summary>
		/// Emits a tail-call instruction
		/// </summary>
		/// <remarks>Should be followed by a Call/CallI/CallVirt</remarks>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper TailCall
		{
			get { _Generator.Emit(OpCodes.Tailcall); return this; }
		}
		
		//****************************************
		// Load/Store Values

		/// <summary>
		/// Emits an instruction to load an argument
		/// </summary>
		/// <param name="argIndex">The index of the argument to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdArg(int argIndex)
		{
			switch (argIndex)
			{
			case 0:
				_Generator.Emit(OpCodes.Ldarg_0);
				break;
					
			case 1:
				_Generator.Emit(OpCodes.Ldarg_1);
				break;
				
			case 2:
				_Generator.Emit(OpCodes.Ldarg_2);
				break;
				
			case 3:
				_Generator.Emit(OpCodes.Ldarg_3);
				break;
			
			default:
				if (argIndex <= byte.MaxValue)
					_Generator.Emit(OpCodes.Ldarg_S, (byte)argIndex);
				else if (argIndex <= short.MaxValue)
					_Generator.Emit(OpCodes.Ldarg, (short)argIndex);
				else
					throw new ArgumentOutOfRangeException("argIndex", "Argument Index is not valid");

				break;
			}

			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of an argument
		/// </summary>
		/// <param name="argIndex">The index of the argument to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdArgA(int argIndex)
		{
			if (argIndex <= byte.MaxValue)
				_Generator.Emit(OpCodes.Ldarga_S, (byte)argIndex);
			else if (argIndex <= short.MaxValue)
				_Generator.Emit(OpCodes.Ldarga, (short)argIndex);
			else
				throw new ArgumentOutOfRangeException("argIndex", "Argument Index is not valid");

			return this;
		}

		/// <summary>
		/// Emits an instruction to load a constant value
		/// </summary>
		/// <param name="boolValue">The boolean value to load</param>
		/// <returns>Self</returns>
		public EmitHelper Ldc(bool boolValue)
		{
			_Generator.Emit(boolValue ? OpCodes.Ldc_I4_1: OpCodes.Ldc_I4_0);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a constant value
		/// </summary>
		/// <param name="intValue">The number to load</param>
		/// <returns>Self</returns>
		public EmitHelper Ldc(int intValue)
		{
			switch (intValue)
			{
			case -1: _Generator.Emit(OpCodes.Ldc_I4_M1); break;
			case 0:  _Generator.Emit(OpCodes.Ldc_I4_0);  break;
			case 1:  _Generator.Emit(OpCodes.Ldc_I4_1);  break;
			case 2:  _Generator.Emit(OpCodes.Ldc_I4_2);  break;
			case 3:  _Generator.Emit(OpCodes.Ldc_I4_3);  break;
			case 4:  _Generator.Emit(OpCodes.Ldc_I4_4);  break;
			case 5:  _Generator.Emit(OpCodes.Ldc_I4_5);  break;
			case 6:  _Generator.Emit(OpCodes.Ldc_I4_6);  break;
			case 7:  _Generator.Emit(OpCodes.Ldc_I4_7);  break;
			case 8:  _Generator.Emit(OpCodes.Ldc_I4_8);  break;
			default:
				if (intValue >= sbyte.MinValue && intValue <= sbyte.MaxValue)
					_Generator.Emit(OpCodes.Ldc_I4_S, (sbyte)intValue);
				else
					_Generator.Emit(OpCodes.Ldc_I4, intValue);
				break;
			}
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a constant value
		/// </summary>
		/// <param name="longValue">The long number to load</param>
		/// <returns>Self</returns>
		public EmitHelper Ldc(long longValue)
		{
			_Generator.Emit(OpCodes.Ldc_I8, longValue);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a constant value
		/// </summary>
		/// <param name="floatValue">The float32 number to load</param>
		/// <returns>Self</returns>
		public EmitHelper Ldc(float floatValue)
		{
			_Generator.Emit(OpCodes.Ldc_R4, floatValue);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a constant value
		/// </summary>
		/// <param name="doubleValue">The float64 number to load</param>
		/// <returns>Self</returns>
		public EmitHelper Ldc(double doubleValue)
		{
			_Generator.Emit(OpCodes.Ldc_R8, doubleValue);
			
			return this;
		}
		
		/// <summary>
		/// Emits the instructions to create a constant decimal structure
		/// </summary>
		/// <param name="decimalValue">The constant to load</param>
		/// <returns>Self</returns>
		/// <remarks>
		/// <para>Emits the values of Decimal.GetBits and passes them to the binary constructor</para>
		/// <para>Optimises for Zero, One, MinusOne, MinValue and MaxValue, and integer values</para>
		/// </remarks>
		public EmitHelper Ldc(decimal decimalValue)
		{
			if (decimalValue == decimal.Zero)
				return this.LdsFld(typeof(decimal).GetField("Zero"));
			
			if (decimalValue == decimal.One)
				return this.LdsFld(typeof(decimal).GetField("One"));

			if (decimalValue == decimal.MinusOne)
				return this.LdsFld(typeof(decimal).GetField("MinusOne"));

			if (decimalValue == decimal.MaxValue)
				return this.LdsFld(typeof(decimal).GetField("MaxValue"));

			if (decimalValue == decimal.MinValue)
				return this.LdsFld(typeof(decimal).GetField("MinValue"));

			if (decimal.Truncate(decimalValue) == decimalValue)
			{
				// Int32 range
				if (decimalValue <= (decimal)int.MaxValue && decimalValue >= (decimal)int.MinValue)
				{
					return this
						.Ldc((int)decimalValue)
						.NewObj(typeof(decimal), typeof(int));
				}
				
				// UInt32 range
				if (decimalValue <= (decimal)ulong.MaxValue && decimalValue > decimal.Zero)
				{
					return this
						.Ldc(unchecked((int)decimal.ToUInt32(decimalValue)))
						.NewObj(typeof(decimal), typeof(uint));
				}
				
				// Int64 range
				if (decimalValue <= (decimal)long.MaxValue && decimalValue >= (decimal)long.MinValue)
				{
					return this
						.Ldc((long)decimalValue)
						.NewObj(typeof(decimal), typeof(long));
				}
				
				// UInt64 range
				if (decimalValue <= (decimal)ulong.MaxValue && decimalValue > decimal.Zero)
				{
					return this
						.Ldc(unchecked((long)decimal.ToUInt64(decimalValue)))
						.NewObj(typeof(decimal), typeof(ulong));
				}
			}
			
			var MyBits = decimal.GetBits(decimalValue);
			
			return this
				.Ldc(MyBits[0])
				.Ldc(MyBits[1])
				.Ldc(MyBits[2])
				.Ldc((MyBits[3] & 0x80000000) == 0x80000000) // Extract the Sign
				.Ldc((byte)((MyBits[3] & 0x00FF0000) >> 16)) // Extract the Scale
				.NewObj(typeof(decimal), typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte));
		}
		
		/// <summary>
		/// Emits the instructions to create a constant DateTime structure
		/// </summary>
		/// <param name="dateTimeValue">The constant to load</param>
		/// <returns>Self</returns>
		/// <remarks>
		/// <para>Emits the values of DateTime.Ticks and DateTime.Kind and passes them to the DateTime constructor</para>
		/// <para>Optimises for MinValue and MaxValue</para>
		/// </remarks>
		public EmitHelper Ldc(DateTime dateTimeValue)
		{
			if (dateTimeValue == DateTime.MinValue)
				return this.LdsFld(typeof(DateTime).GetField("MinValue"));
			
			if (dateTimeValue == DateTime.MaxValue)
				return this.LdsFld(typeof(DateTime).GetField("MaxValue"));
			
			return this
				.Ldc(dateTimeValue.Ticks)
				.Ldc((int)dateTimeValue.Kind)
				.NewObj(typeof(DateTime), typeof(long), typeof(DateTimeKind));
		}
		
		/// <summary>
		/// Emits the instructions to create a constant TimeSpan structure
		/// </summary>
		/// <param name="timeSpanValue">The constant to load</param>
		/// <returns>Self</returns>
		/// <remarks>
		/// <para>Emits the value of TimeSpan.Ticks and passes it to the TimeSpan constructor</para>
		/// <para>Optimises for Zero, MinValue, and MaxValue</para>
		/// </remarks>
		public EmitHelper Ldc(TimeSpan timeSpanValue)
		{
			if (timeSpanValue == TimeSpan.Zero)
				return this.LdsFld(typeof(TimeSpan).GetField("Zero"));
			
			if (timeSpanValue == TimeSpan.MinValue)
				return this.LdsFld(typeof(TimeSpan).GetField("MinValue"));
			
			if (timeSpanValue == TimeSpan.MaxValue)
				return this.LdsFld(typeof(TimeSpan).GetField("MaxValue"));
			
			return this
				.Ldc(timeSpanValue.Ticks)
				.NewObj(typeof(TimeSpan), typeof(long));
		}
		
		/// <summary>
		/// Emits the instructions to place a constant on the stack
		/// </summary>
		/// <param name="value">The value to place</param>
		/// <returns>Self</returns>
		/// <remarks>Dynamically chooses the IL based on the given type. Intended for generic classes outputting values where the type isn't known at compile time</remarks>
		/// <exception cref="NotSupportedException">Thrown if the type is not a supported core type.</exception>
		public EmitHelper LdcDynamic(object value)
		{
			if (value == null)
				return LdNull;
			
			var MyType = value.GetType();
			
			if (MyType == typeof(bool))
				return Ldc((bool)value);
			
			if (MyType == typeof(int))
				return Ldc((int)value);

			if (MyType == typeof(long))
				return Ldc((long)value);

			if (MyType == typeof(float))
				return Ldc((float)value);

			if (MyType == typeof(double))
				return Ldc((double)value);

			if (MyType == typeof(DateTime))
				return Ldc((DateTime)value);
			
			if (MyType == typeof(TimeSpan))
				return Ldc((TimeSpan)value);
			
			if (MyType == typeof(string))
				return LdStr((string)value);
			
			throw new NotSupportedException("Unsupported Type");
		}
		
		/// <summary>
		/// Emits an instruction to load an object reference from an array
		/// </summary>
		/// <returns>Self</returns>
		public EmitHelper LdElem()
		{
			_Generator.Emit(OpCodes.Ldelem_Ref);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load an element from an array
		/// </summary>
		/// <param name="targetType">The type to load from the array</param>
		/// <returns>Self</returns>
		public EmitHelper LdElem(Type targetType)
		{
			if (targetType.IsValueType)
			{
				switch (Type.GetTypeCode(targetType))
				{
				case TypeCode.Boolean:
				case TypeCode.SByte:  _Generator.Emit(OpCodes.Ldelem_I1); break;
				case TypeCode.Int16:  _Generator.Emit(OpCodes.Ldelem_I2); break;
				case TypeCode.Int32:  _Generator.Emit(OpCodes.Ldelem_I4); break;
				case TypeCode.Int64:  _Generator.Emit(OpCodes.Ldelem_I8); break;
	
				case TypeCode.Byte:   _Generator.Emit(OpCodes.Ldelem_U1); break;
				case TypeCode.Char:
				case TypeCode.UInt16: _Generator.Emit(OpCodes.Ldelem_U2); break;
				case TypeCode.UInt32: _Generator.Emit(OpCodes.Ldelem_U4); break;
//				case TypeCode.UInt64: _Generator.Emit(OpCodes.Ldelem_U8); break;
	
				case TypeCode.Single: _Generator.Emit(OpCodes.Ldelem_R4); break;
				case TypeCode.Double: _Generator.Emit(OpCodes.Ldelem_R8); break;
	
				default:
					_Generator.Emit(OpCodes.Ldelem, targetType);
					break;
				}
				
				return this;
			}
			
			_Generator.Emit(OpCodes.Ldelem, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of an array element as a managed pointer
		/// </summary>
		/// <param name="targetType">The type of the array element</param>
		/// <returns>Self</returns>
		public EmitHelper LdElemA(Type targetType)
		{
			_Generator.Emit(OpCodes.Ldelema, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of an array element as a native pointer
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper LdElemI
		{
			get { _Generator.Emit(OpCodes.Ldelem_I); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to load the value of a field
		/// </summary>
		/// <param name="sourceField">The field type to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdFld(FieldInfo sourceField)
		{
			_Generator.Emit(OpCodes.Ldfld, sourceField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the value of a field
		/// </summary>
		/// <param name="targetType">The type owning the field to load</param>
		/// <param name="targetName">The name of the field to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdFld(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(OpCodes.Ldfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a field as a managed pointer
		/// </summary>
		/// <param name="sourceField">The field to find the address of</param>
		/// <returns>Self</returns>
		public EmitHelper LdFldA(FieldInfo sourceField)
		{
			_Generator.Emit(OpCodes.Ldflda, sourceField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a field as a managed pointer
		/// </summary>
		/// <param name="targetType">The type owning the field to load</param>
		/// <param name="targetName">The name of the field to find the address of</param>
		/// <returns>Self</returns>
		public EmitHelper LdFldA(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			_Generator.Emit(OpCodes.Ldflda, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a native pointer to a method body
		/// </summary>
		/// <param name="targetMethod">The method to find the address of</param>
		/// <returns>Self</returns>
		public EmitHelper LdFtN(MethodInfo targetMethod)
		{
			_Generator.Emit(OpCodes.Ldftn, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a value from an address
		/// </summary>
		/// <param name="targetType">The type to read from the address</param>
		/// <returns>Self</returns>
		public EmitHelper LdInd(Type targetType)
		{
			if (targetType.IsValueType)
			{
				switch (Type.GetTypeCode(targetType))
				{
				case TypeCode.Boolean:
				case TypeCode.SByte:  _Generator.Emit(OpCodes.Ldind_I1); break;
				case TypeCode.Int16:  _Generator.Emit(OpCodes.Ldind_I2); break;
				case TypeCode.Int32:  _Generator.Emit(OpCodes.Ldind_I4); break;
				case TypeCode.Int64:  _Generator.Emit(OpCodes.Ldind_I8); break;
	
				case TypeCode.Byte:   _Generator.Emit(OpCodes.Ldind_U1); break;
				case TypeCode.Char:
				case TypeCode.UInt16: _Generator.Emit(OpCodes.Ldind_U2); break;
				case TypeCode.UInt32: _Generator.Emit(OpCodes.Ldind_U4); break;
				case TypeCode.UInt64: _Generator.Emit(OpCodes.Ldind_I8); break;
	
				case TypeCode.Single: _Generator.Emit(OpCodes.Ldind_R4); break;
				case TypeCode.Double: _Generator.Emit(OpCodes.Ldind_R8); break;
	
				default:
					_Generator.Emit(OpCodes.Ldobj, targetType);
					break;
				}
				
				return this;
			}
			
			if (targetType.IsClass)
				_Generator.Emit(OpCodes.Ldind_Ref);
			else
				throw new ArgumentException("Type cannot be loaded");
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the length of an array
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper LdLen
		{
			get { _Generator.Emit(OpCodes.Ldlen); return this; }
		}

		/// <summary>
		/// Emits an instruction to load a Local Variable
		/// </summary>
		/// <param name="localName">The named Local Variable</param>
		/// <returns>Self</returns>
		public EmitHelper LdLoc(string localName)
		{
			_Generator.Emit(OpCodes.Ldloc, GetLocal(localName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a Local Variable
		/// </summary>
		/// <param name="local">The Local Variable</param>
		/// <returns>Self</returns>
		public EmitHelper LdLoc(LocalBuilder local)
		{
			_Generator.Emit(OpCodes.Ldloc, local);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a Local Variable
		/// </summary>
		/// <param name="localName">The named Local Variable</param>
		/// <returns>Self</returns>
		public EmitHelper LdLocA(string localName)
		{
			_Generator.Emit(OpCodes.Ldloca, GetLocal(localName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a Local Variable
		/// </summary>
		/// <param name="local">The Local Variable</param>
		/// <returns>Self</returns>
		public EmitHelper LdLocA(LocalBuilder local)
		{
			_Generator.Emit(OpCodes.Ldloca, local);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a null value
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper LdNull
		{
			get { _Generator.Emit(OpCodes.Ldnull); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to load an object pointed to by a managed pointer
		/// </summary>
		/// <param name="targetType">The type of the object being pointed to</param>
		/// <returns>Self</returns>
		public EmitHelper LdObj(Type targetType)
		{
			_Generator.Emit(OpCodes.Ldobj, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a static field
		/// </summary>
		/// <param name="sourceField">The source static field to read from</param>
		/// <returns>Self</returns>
		public EmitHelper LdsFld(FieldInfo sourceField)
		{
			if (!sourceField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", sourceField.DeclaringType.FullName, sourceField.Name));

			_Generator.Emit(OpCodes.Ldsfld, sourceField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a static field
		/// </summary>
		/// <param name="targetType">The type owning the static field to load</param>
		/// <param name="targetName">The name of the static field to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdsFld(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			if (!targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", targetType.FullName, targetName));
			
			_Generator.Emit(OpCodes.Ldsfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a static field
		/// </summary>
		/// <param name="sourceField">The static field to load the address of</param>
		/// <returns>Self</returns>
		public EmitHelper LdsFldA(FieldInfo sourceField)
		{
			if (!sourceField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", sourceField.DeclaringType.FullName, sourceField.Name));

			_Generator.Emit(OpCodes.Ldsflda, sourceField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load the address of a static field
		/// </summary>
		/// <param name="targetType">The type owning the static field to load</param>
		/// <param name="targetName">The name of the static field to load the address of</param>
		/// <returns>Self</returns>
		public EmitHelper LdsFldA(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			if (!targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", targetType.FullName, targetName));
			
			_Generator.Emit(OpCodes.Ldsflda, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a constant string, or null if no string is supplied
		/// </summary>
		/// <param name="sourceString">The source string to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdStr(string sourceString)
		{
			if (sourceString == null)
				_Generator.Emit(OpCodes.Ldnull);
			else
				_Generator.Emit(OpCodes.Ldstr, sourceString);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a method token
		/// </summary>
		/// <param name="targetMethod">The method to retrieve the token for</param>
		/// <returns>Self</returns>
		public EmitHelper LdToken(MethodInfo targetMethod)
		{
			_Generator.Emit(OpCodes.Ldtoken, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a field token
		/// </summary>
		/// <param name="targetField">The field to retrieve the token for</param>
		/// <returns>Self</returns>
		public EmitHelper LdToken(FieldInfo targetField)
		{
			_Generator.Emit(OpCodes.Ldtoken, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a type token
		/// </summary>
		/// <param name="targetType">The type to retrieve the token for</param>
		/// <returns>Self</returns>
		public EmitHelper LdToken(Type targetType)
		{
			_Generator.Emit(OpCodes.Ldtoken, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to load a native pointer to a virtual method on a specific object
		/// </summary>
		/// <param name="targetMethod">The virtual method to load</param>
		/// <returns>Self</returns>
		public EmitHelper LdVirtFtN(MethodInfo targetMethod)
		{
			_Generator.Emit(OpCodes.Ldvirtftn, targetMethod);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to pop a value off the stack
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Pop
		{
			get { _Generator.Emit(OpCodes.Pop); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to store a value to an argument
		/// </summary>
		/// <param name="argIndex">The index of the argument to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StArg(int argIndex)
		{
			if (argIndex < byte. MaxValue)
				_Generator.Emit(OpCodes.Starg_S, (byte)argIndex);
			else if (argIndex < short.MaxValue)
				_Generator.Emit(OpCodes.Starg, (short)argIndex);
			else
				throw new ArgumentOutOfRangeException("argIndex", "Argument Index is not valid");

			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to an array element
		/// </summary>
		/// <param name="targetType">The type of the array to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StElem(Type targetType)
		{
			if (targetType.IsValueType)
			{
				switch (Type.GetTypeCode(targetType))
				{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:  _Generator.Emit(OpCodes.Stelem_I1); break;
				case TypeCode.Char:
				case TypeCode.UInt16:
				case TypeCode.Int16:  _Generator.Emit(OpCodes.Stelem_I2); break;
				case TypeCode.UInt32:
				case TypeCode.Int32:  _Generator.Emit(OpCodes.Stelem_I4); break;
				case TypeCode.UInt64:
				case TypeCode.Int64:  _Generator.Emit(OpCodes.Stelem_I8); break;
	
				case TypeCode.Single: _Generator.Emit(OpCodes.Stelem_R4); break;
				case TypeCode.Double: _Generator.Emit(OpCodes.Stelem_R8); break;
	
				default:
					_Generator.Emit(OpCodes.Stelem, targetType);
					break;
				}
				
				return this;
			}
			
			if (targetType == typeof(object))
				_Generator.Emit(OpCodes.Stelem_Ref);
			else
				_Generator.Emit(OpCodes.Stelem, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to an object field
		/// </summary>
		/// <param name="targetField">The target field</param>
		/// <returns>Self</returns>
		public EmitHelper StFld(FieldInfo targetField)
		{
			if (targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is static", targetField.DeclaringType.FullName, targetField.Name));
			
			_Generator.Emit(OpCodes.Stfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to an object field
		/// </summary>
		/// <param name="targetType">The type owning the field to store into</param>
		/// <param name="targetName">The name of the field to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StFld(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));

			if (targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is static", targetType.FullName, targetName));
			
			_Generator.Emit(OpCodes.Stfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to an address
		/// </summary>
		/// <param name="targetType">The type of the value to store</param>
		/// <returns>Self</returns>
		public EmitHelper StInd(Type targetType)
		{
			if (targetType.IsValueType)
			{
				switch (Type.GetTypeCode(targetType))
				{
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:  _Generator.Emit(OpCodes.Stind_I1); break;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:  _Generator.Emit(OpCodes.Stind_I2); break;
				case TypeCode.Int32:
				case TypeCode.UInt32:  _Generator.Emit(OpCodes.Stind_I4); break;
				case TypeCode.Int64:
				case TypeCode.UInt64:  _Generator.Emit(OpCodes.Stind_I8); break;
	
				case TypeCode.Single: _Generator.Emit(OpCodes.Ldind_R4); break;
				case TypeCode.Double: _Generator.Emit(OpCodes.Ldind_R8); break;
	
				default:
					_Generator.Emit(OpCodes.Stobj, targetType);
					break;
				}
				
				return this;
			}
			
			if (targetType.IsClass)
				_Generator.Emit(OpCodes.Stind_Ref);
			else
				throw new ArgumentException("Type cannot be stored");
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to a Local Variable
		/// </summary>
		/// <param name="localName">The named Local Variable to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StLoc(string localName)
		{
			_Generator.Emit(OpCodes.Stloc, GetLocal(localName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to a Local Variable
		/// </summary>
		/// <param name="local">The Local Variable to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StLoc(LocalBuilder local)
		{
			_Generator.Emit(OpCodes.Stloc, local);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store an object to a given memory address
		/// </summary>
		/// <param name="targetType">The type of object to store</param>
		/// <returns>Self</returns>
		public EmitHelper StObj(Type targetType)
		{
			_Generator.Emit(OpCodes.Stobj, targetType);
			
			return this;
		}

		/// <summary>
		/// Emits an instruction to store a value into a static field
		/// </summary>
		/// <param name="targetField">The target field to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StsFld(FieldInfo targetField)
		{
			if (!targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", targetField.DeclaringType.FullName, targetField.Name));
			
			_Generator.Emit(OpCodes.Stsfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to store a value to a static field
		/// </summary>
		/// <param name="targetType">The type owning the field to store into</param>
		/// <param name="targetName">The name of the field to store to</param>
		/// <returns>Self</returns>
		public EmitHelper StsFld(Type targetType, string targetName)
		{
			FieldInfo targetField = GetField(targetType, targetName);

			if (targetField == null)
				throw new ArgumentException(string.Format("{0}.{1} does not exist, or does not have the correct parameters", targetType.FullName, targetName));
			
			if (!targetField.IsStatic)
				throw new ArgumentException(string.Format("{0}.{1} is not static", targetType.FullName, targetName));

			_Generator.Emit(OpCodes.Stsfld, targetField);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction marking an address as unaligned
		/// </summary>
		/// <param name="labelName">The named Label to align to</param>
		/// <returns>Self</returns>
		public EmitHelper Unaligned(string labelName)
		{
			_Generator.Emit(OpCodes.Unaligned, GetLabel(labelName));
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction marking an address as unaligned
		/// </summary>
		/// <param name="label">The Label to align to</param>
		/// <returns>Self</returns>
		public EmitHelper Unaligned(Label label)
		{
			_Generator.Emit(OpCodes.Unaligned, label);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction marking an address as unaligned
		/// </summary>
		/// <param name="unalignedAddress">The address to align to</param>
		/// <returns>Self</returns>
		public EmitHelper Unaligned(long unalignedAddress)
		{
			_Generator.Emit(OpCodes.Unaligned, unalignedAddress);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to mark the next access as volatile
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Volatile
		{
			get { _Generator.Emit(OpCodes.Volatile); return this; }
		}
		
		//****************************************
		// Type Management
		
		/// <summary>
		/// Emits an instruction to box a value type
		/// </summary>
		/// <param name="valueType">The value type to box</param>
		/// <returns>Self</returns>
		public EmitHelper Box(Type valueType)
		{
			_Generator.Emit(OpCodes.Box, valueType);
			
			return this;
		}
		
		/// <summary>
		/// Conditionally emits a Box instruction if the given type is a value type
		/// </summary>
		/// <param name="valueType">The type to box</param>
		/// <returns>Self</returns>
		public EmitHelper BoxIfValueType(Type valueType)
		{
			if (valueType.IsValueType)
				_Generator.Emit(OpCodes.Box, valueType);
			
			return this;
		}
		
		/// <summary>
		/// Conditionally emits a Box instruction if the given type is a value type and the condition is true
		/// </summary>
		/// <param name="valueType">The type to box</param>
		/// <param name="condition">The condition result</param>
		/// <returns>Self</returns>
		public EmitHelper BoxIf(Type valueType, bool condition)
		{
			if (valueType.IsValueType && condition)
				_Generator.Emit(OpCodes.Box, valueType);
			
			return this;
		}
		
		/// <summary>
		/// Emits a Castclass
		/// </summary>
		/// <param name="targetType">The type to cast</param>
		/// <returns>Self</returns>
		public EmitHelper CastClass(Type targetType)
		{
			_Generator.Emit(OpCodes.Castclass, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits Unbox_Any for value types, and Castclass for reference types
		/// </summary>
		/// <param name="targetType"></param>
		/// <returns>Self</returns>
		public EmitHelper CastType(Type targetType)
		{
			if (targetType.IsValueType)
				_Generator.Emit(OpCodes.Unbox_Any, targetType);
			else
				_Generator.Emit(OpCodes.Castclass, targetType);

			return this;
		}		
		
		/// <summary>
		/// Emits an appropriate conversion instruction for a primitive type
		/// </summary>
		/// <param name="targetType">The primitive type to convert to</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertTo(Type targetType)
		{
			switch (Type.GetTypeCode(targetType))
			{
			case TypeCode.Boolean:
			case TypeCode.SByte:  _Generator.Emit(OpCodes.Conv_I1); break;
			case TypeCode.Int16:  _Generator.Emit(OpCodes.Conv_I2); break;
			case TypeCode.Int32:  _Generator.Emit(OpCodes.Conv_I4); break;
			case TypeCode.Int64:  _Generator.Emit(OpCodes.Conv_I8); break;

			case TypeCode.Byte:   _Generator.Emit(OpCodes.Conv_U1); break;
			case TypeCode.Char:
			case TypeCode.UInt16: _Generator.Emit(OpCodes.Conv_U2); break;
			case TypeCode.UInt32: _Generator.Emit(OpCodes.Conv_U4); break;
			case TypeCode.UInt64: _Generator.Emit(OpCodes.Conv_U8); break;

			case TypeCode.Single: _Generator.Emit(OpCodes.Conv_R4); break;
			case TypeCode.Double: _Generator.Emit(OpCodes.Conv_R8); break;

			default:
				throw new ArgumentException("Type cannot be converted");
			}

			return this;
		}
		
		/// <summary>
		/// Emits an appropriate signed overflow conversion instruction for a primitive type
		/// </summary>
		/// <param name="targetType">The primitive type to convert to</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertSignedOverflow(Type targetType)
		{
			switch (Type.GetTypeCode(targetType))
			{
			case TypeCode.Boolean:
			case TypeCode.SByte:  _Generator.Emit(OpCodes.Conv_Ovf_I1); break;
			case TypeCode.Int16:  _Generator.Emit(OpCodes.Conv_Ovf_I2); break;
			case TypeCode.Int32:  _Generator.Emit(OpCodes.Conv_Ovf_I4); break;
			case TypeCode.Int64:  _Generator.Emit(OpCodes.Conv_Ovf_I8); break;

			case TypeCode.Byte:   _Generator.Emit(OpCodes.Conv_Ovf_U1); break;
			case TypeCode.Char:
			case TypeCode.UInt16: _Generator.Emit(OpCodes.Conv_Ovf_U2); break;
			case TypeCode.UInt32: _Generator.Emit(OpCodes.Conv_Ovf_U4); break;
			case TypeCode.UInt64: _Generator.Emit(OpCodes.Conv_Ovf_U8); break;

			case TypeCode.Single: _Generator.Emit(OpCodes.Conv_R4); break;
			case TypeCode.Double: _Generator.Emit(OpCodes.Conv_R8); break;

			default:
				throw new ArgumentException("Type cannot be converted");
			}

			return this;
		}
		
		/// <summary>
		/// Emits an appropriate unsigned overflow conversion instruction for a primitive type
		/// </summary>
		/// <param name="targetType">The primitive type to convert to</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertUnsignedOverflow(Type targetType)
		{
			switch (Type.GetTypeCode(targetType))
			{
			case TypeCode.Boolean:
			case TypeCode.SByte:  _Generator.Emit(OpCodes.Conv_Ovf_I1_Un); break;
			case TypeCode.Int16:  _Generator.Emit(OpCodes.Conv_Ovf_I2_Un); break;
			case TypeCode.Int32:  _Generator.Emit(OpCodes.Conv_Ovf_I4_Un); break;
			case TypeCode.Int64:  _Generator.Emit(OpCodes.Conv_Ovf_I8_Un); break;

			case TypeCode.Byte:   _Generator.Emit(OpCodes.Conv_Ovf_U1_Un); break;
			case TypeCode.Char:
			case TypeCode.UInt16: _Generator.Emit(OpCodes.Conv_Ovf_U2_Un); break;
			case TypeCode.UInt32: _Generator.Emit(OpCodes.Conv_Ovf_U4_Un); break;
			case TypeCode.UInt64: _Generator.Emit(OpCodes.Conv_Ovf_U8_Un); break;

			case TypeCode.Single: _Generator.Emit(OpCodes.Conv_R4); break;
			case TypeCode.Double: _Generator.Emit(OpCodes.Conv_R8); break;

			default:
				throw new ArgumentException("Type cannot be converted");
			}

			return this;
		}
		
		/// <summary>
		/// Emits an instruction to convert a signed value to a native integer
		/// </summary>
		/// <param name="signed">True to perform a signed conversion, otherwise false</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertNatural(bool signed)
		{
			if (signed)
				_Generator.Emit(OpCodes.Conv_I);
			else
				_Generator.Emit(OpCodes.Conv_U);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to convert a signed value to a native integer with overflow
		/// </summary>
		/// <param name="signed">True to perform a signed conversion, otherwise false</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertNaturalOverflow(bool signed)
		{
			if (signed)
				_Generator.Emit(OpCodes.Conv_Ovf_I);
			else
				_Generator.Emit(OpCodes.Conv_Ovf_U);
			
			return this;
		}

		/// <summary>
		/// Emits an instruction to convert an unsigned value to a native integer with overflow
		/// </summary>
		/// <param name="signed">True to perform a signed conversion, otherwise false</param>
		/// <returns>Self</returns>
		public EmitHelper ConvertNaturalUnsigned(bool signed)
		{
			if (signed)
				_Generator.Emit(OpCodes.Conv_Ovf_I_Un);
			else
				_Generator.Emit(OpCodes.Conv_Ovf_U_Un);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to convert an unsigned value to a float32
		/// </summary>
		public EmitHelper ConvertToFloat
		{
			get { _Generator.Emit(OpCodes.Conv_R_Un); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to check if the value is an instance of a type
		/// </summary>
		/// <param name="targetType">The type to check against</param>
		/// <returns>Self</returns>
		public EmitHelper IsInst(Type targetType)
		{
			_Generator.Emit(OpCodes.Isinst, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to convert a boxed value type to its unboxed value
		/// </summary>
		/// <param name="valueType">The expected resulting type</param>
		/// <returns>Self</returns>
		public EmitHelper Unbox(Type valueType)
		{
			_Generator.Emit(OpCodes.Unbox, valueType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to convert a boxed value to its unboxed value, or castclass if the value is a reference type
		/// </summary>
		/// <param name="valueType">The expected resulting type</param>
		/// <returns>Self</returns>
		public EmitHelper UnboxAny(Type valueType)
		{
			_Generator.Emit(OpCodes.Unbox_Any, valueType);
			
			return this;
		}
	
		/// <summary>
		/// Conditionally emits an instruction to convert a boxed value to its unboxed value, only if the type is a value type
		/// </summary>
		/// <param name="valueType">The expected resulting type</param>
		/// <returns>Self</returns>
		public EmitHelper UnboxIfValueType(Type valueType)
		{
			if (valueType.IsValueType)
				_Generator.Emit(OpCodes.Unbox_Any, valueType);
			
			return this;
		}
		
		//****************************************
		// General
		
		/// <summary>
		/// Emits an instruction to load the address to the argument list of the current method
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper ArgList
		{
			get { _Generator.Emit(OpCodes.Arglist); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to copy a set of bytes from a source address to a destination address
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper CpBlock
		{
			get { _Generator.Emit(OpCodes.Cpblk); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to copy a value type from one address to another
		/// </summary>
		/// <param name="targetType">The value type to copy</param>
		/// <returns>Self</returns>
		public EmitHelper CpObj(Type targetType)
		{
			_Generator.Emit(OpCodes.Cpobj, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to duplicate the value on the stack
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Dup
		{
			get { _Generator.Emit(OpCodes.Dup); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to initialise a block of memory
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper InitBlk
		{
			get { _Generator.Emit(OpCodes.Initblk); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to initialise an value types fields to default values
		/// </summary>
		/// <param name="targetType">The value type to set</param>
		/// <returns>Self</returns>
		public EmitHelper InitObj(Type targetType)
		{
			_Generator.Emit(OpCodes.Initobj, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to allocate a set of bytes from the dynamic memory pool
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper LocAlloc
		{
			get { _Generator.Emit(OpCodes.Localloc); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to load a typed reference to a given instance
		/// </summary>
		/// <param name="targetType">The type of object to load</param>
		/// <returns>Self</returns>
		public EmitHelper MkRefAny(Type targetType)
		{
			_Generator.Emit(OpCodes.Mkrefany, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to create a new array of the requested type
		/// </summary>
		/// <param name="targetType">The type of array element to use</param>
		/// <returns>Self</returns>
		public EmitHelper NewArr(Type targetType)
		{
			_Generator.Emit(OpCodes.Newarr, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to create a new object, using the provided constructor
		/// </summary>
		/// <param name="constructorInfo">The constructor to execute</param>
		/// <returns>Self</returns>
		public EmitHelper NewObj(ConstructorInfo constructorInfo)
		{
			_Generator.Emit(OpCodes.Newobj, constructorInfo);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to create a new object, using the default constructor
		/// </summary>
		/// <param name="type">The type to create</param>
		/// <returns>Self</returns>
		public EmitHelper NewObj(Type type)
		{
			ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			
			if (constructorInfo == null)
				throw new MissingMemberException(string.Format("Type {0} does not have a default constructor", type.FullName));

			_Generator.Emit(OpCodes.Newobj, constructorInfo);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to create a new object, using the specified constructor arguments
		/// </summary>
		/// <param name="type">The type to create</param>
		/// <param name="parameters">The arguments for the targeted constructor</param>
		/// <returns>Self</returns>
		public EmitHelper NewObj(Type type, params Type[] parameters)
		{
			ConstructorInfo constructorInfo = type.GetConstructor(parameters);

			_Generator.Emit(OpCodes.Newobj, constructorInfo);
			
			return this;
		}

		/// <summary>
		/// Emits an instruction to mark an array address (ldelema) operation as not performing a type check
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper ReadOnly
		{
			get { _Generator.Emit(OpCodes.Readonly); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to retrieve the type from a typed reference
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper RefAnyType
		{
			get { _Generator.Emit(OpCodes.Refanytype); return this; }
		}
		
		/// <summary>
		/// Emits an instruction to retrieve the address from a typed reference
		/// </summary>
		/// <param name="targetType">The type of reference to retrieve</param>
		/// <returns>Self</returns>
		public EmitHelper RefAnyVal(Type targetType)
		{
			_Generator.Emit(OpCodes.Refanyval, targetType);
			
			return this;
		}
		
		/// <summary>
		/// Emits an instruction to return the size in bytes of a given value type
		/// </summary>
		/// <param name="targetType">The target value type</param>
		/// <returns>Self</returns>
		public EmitHelper @SizeOf(Type targetType)
		{
			_Generator.Emit(OpCodes.Sizeof, targetType);
			
			return this;
		}
				
		//****************************************
		// Debugging
		
		/// <summary>
		/// Emits an instruction to break in the debugger
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EmitHelper Break
		{
			get { _Generator.Emit(OpCodes.Break); return this; }
		}
		
		/// <summary>
		/// Emits an instruction that does nothing
		/// </summary>
		public EmitHelper Nop
		{
			get { _Generator.Emit(OpCodes.Nop); return this; }
		}
		
		//****************************************
		
		/// <summary>
		/// Ends a sequence of emitted instructions
		/// </summary>
		public void End()
		{
			
		}
		
		/// <summary>
		/// Creates a delegate that represents the underlying Dynamic Method
		/// </summary>
		/// <returns>The requeste delegate</returns>
		public TDelegate ToDelegate<TDelegate>() where TDelegate : class
		{
			return _DestMethod.CreateDelegate(typeof(TDelegate)) as TDelegate;
		}
		
		/// <summary>
		/// Creates a delegate that represents the underlying Dynamic Method
		/// </summary>
		/// <param name="target">The target object to bind to</param>
		/// <returns></returns>
		public TDelegate ToDelegate<TDelegate>(object target) where TDelegate : class
		{
			return _DestMethod.CreateDelegate(typeof(TDelegate), target) as TDelegate;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the underlying IL Generator
		/// </summary>
		public ILGenerator Generator
		{
			get { return _Generator; }
		}
		
		/// <summary>
		/// Gets the underlying Dynamic Method
		/// </summary>
		public DynamicMethod Method
		{
			get { return _DestMethod; }
		}
		
		//****************************************
		
		private Label GetLabel(string labelName)
		{	//****************************************
			Label MyLabel;
			//****************************************
			
			if (_Labels.TryGetValue(labelName, out MyLabel))
				return MyLabel;

			throw new ArgumentException("Label does not exist");
		}
				
		private LocalBuilder GetLocal(string localName)
		{	//****************************************
			LocalBuilder MyLocal;
			//****************************************
			
			if (_Locals.TryGetValue(localName, out MyLocal))
				return MyLocal;

			throw new ArgumentException("Local does not exist");
		}
		
	}
}
#endif