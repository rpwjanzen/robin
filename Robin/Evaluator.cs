namespace Robin.Eval
{
    using System;
    using Robin.Ast;
    using Robin.Obj;
    using Environment = Robin.Environment;

    class Evaluator
    {
        public IObject Eval(INode node, Environment env)
        {
            if (node is Program program)
            {
                return EvalProgram(program, env);
            }
            else if (node is BlockStatement bStmt)
            {
                return EvalBlockStatement(bStmt, env);
            }
            else if (node is ExpressionStatement exprStmt)
            {
                return Eval(exprStmt.Expression, env);
            }
            else if (node is IntegerLiteral literal)
            {
                return new Integer { Value = literal.Value };
            }
            else if (node is Ast.Boolean boolean)
            {
                return NativeBoolToBooleanObject(boolean.Value);
            }
            else if (node is Null)
            {
                return Null.Instance;
            }
            else if (node is PrefixExpression prefixExpression)
            {
                var right = Eval(prefixExpression.Right, env);
                if (IsError(right))
                {
                    return right;
                }

                return EvalPrefixExpression(prefixExpression.Operator, right);
            }
            else if (node is InfixExpression infixExpr)
            {
                var left = Eval(infixExpr.Left, env);
                if (IsError(left))
                {
                    return left;
                }

                var right = Eval(infixExpr.Right, env);
                if (IsError(right))
                {
                    return right;
                }

                return EvalInfixExpression(infixExpr.Operator, left, right);
            }
            else if (node is IfExpression ifExpr)
            {
                return EvalIfExpression(ifExpr, env);
            }
            else if (node is ReturnStatement rs)
            {
                var val = Eval(rs.ReturnValue, env);
                if (IsError(val))
                {
                    return val;
                }

                return new ReturnValue { Value = val };
            }
            else if (node is LetStatement letStmt)
            {
                var val = Eval(letStmt.Value, env);
                if (IsError(val))
                {
                    return val;
                }

                env.Set(letStmt.Name.Value, val);

                // TODO: determine if we want to return val instead
                return null;
            }
            else if (node is Identifier id)
            {
                return EvalIdentifier(id, env);
            }
            else if (node is FunctionLiteral fl)
            {
                return new Function { Parameters = fl.Parameters, Body = fl.Body, Env = env };
            }
            else if (node is CallExpression ce)
            {
                var fn = Eval(ce.Function, env);
                if (IsError(fn))
                {
                    return fn;
                }

                var args = EvalExpressions(ce.Arguments, env);
                if (args.Length == 1 && IsError(args[0]))
                {
                    return args[0];
                }

                if (fn is Function f)
                {
                    return AppyFunction(f, args);
                }

                return NewError($"now a function: {fn.Type()}");
            }
            else
            {
                return null;
            }
        }

        private IObject AppyFunction(Function fn, IObject[] args)
        {
            var extendedEnv = ExtendFunctionEnv(fn, args);
            var evaluated = Eval(fn.Body, extendedEnv);
            return UnwrapReturnValue(evaluated);
        }

        private IObject UnwrapReturnValue(IObject evaluated)
        {
            if (evaluated is ReturnValue rv)
            {
                return rv.Value;
            }

            return evaluated;
        }

        private Environment ExtendFunctionEnv(Function fn, IObject[] args)
        {
            var env = new Environment(fn.Env);
             for (var i = 0; i < fn.Parameters.Length; i++)
            {
                env.Set(fn.Parameters[i].Value, args[i]);
            }

            return env;
        }

        private IObject[] EvalExpressions(IExpression[] arguments, Environment env)
        {
            var result = new System.Collections.Generic.List<IObject>();
            foreach (var e in arguments)
            {
                var ev = Eval(e, env);
                if (IsError(ev))
                {
                    return new IObject[] { ev };
                }
                result.Add(ev);
            }

            return result.ToArray();
        }

        private IObject EvalIdentifier(Identifier id, Environment env)
        {
            if (env.TryGet(id.Value, out IObject obj))
            {
                return obj;
            }
            else
            {
                return NewError($"identifier not found: {id.Value}");
            }
        }

        private IObject EvalBlockStatement(BlockStatement bStmt, Environment env)
        {
            IObject rs = default(IObject);
            foreach (var stmt in bStmt.Statements)
            {
                rs = Eval(stmt, env);
                if (rs != null && (rs.Type() == ObjectType.ReturnValue || rs.Type() == ObjectType.Error))
                {
                    return rs;
                }
            }

            return rs;
        }

        private IObject EvalIfExpression(IfExpression ifExpr, Environment env)
        {
            var condition = Eval(ifExpr.Condition, env);
            if (IsError(condition))
            {
                return condition;
            }

            if (IsTruthy(condition))
            {
                return Eval(ifExpr.Consequent, env);
            }
            else if (ifExpr.Alternative != null)
            {
                return Eval(ifExpr.Alternative, env);
            }
            else
            {
                return Null.Instance;
            }
        }

        private bool IsTruthy(IObject obj)
        {
            if (obj is Null)
            {
                return false;
            }
            else if (obj is Obj.Boolean b && b == Obj.Boolean.False)
            {
                return false;
            }
            else
            {
                // anything else is truthy
                return true;
            }
        }

        private IObject NativeBoolToBooleanObject(bool value)
        {
            return value ? Obj.Boolean.True : Obj.Boolean.False;
        }

        private IObject EvalInfixExpression(string @operator, IObject left, IObject right)
        {
            if (left is Integer li && right is Integer ri)
            {
                return EvalIntegerInfixExpression(@operator, li, ri);
            }

            if (left.Type() != right.Type())
            {
                return NewError($"type mismatch: {left.Type()} {@operator} {right.Type()}");
            }

            switch (@operator)
            {
                case "==":
                    return NativeBoolToBooleanObject(left.Equals(right));
                case "!=":
                    return NativeBoolToBooleanObject(!left.Equals(right));
                default:
                    return NewError($"unknown operator: {@operator}");
            }
        }

        private IObject EvalIntegerInfixExpression(string @operator, Integer li, Integer ri)
        {
            var lv = li.Value;
            var rv = ri.Value;
            switch (@operator)
            {
                case "+":
                    return new Integer { Value = lv + rv };
                case "-":
                    return new Integer { Value = lv - rv };
                case "*":
                    return new Integer { Value = lv * rv };
                case "/":
                    return new Integer { Value = lv / rv };

                case "<":
                    return NativeBoolToBooleanObject(lv < rv);
                case ">":
                    return NativeBoolToBooleanObject(lv > rv);
                case "==":
                    return NativeBoolToBooleanObject(lv == rv);
                case "!=":
                    return NativeBoolToBooleanObject(lv != rv);

                default:
                    return NewError($"unknown operator: {li.Type()} {@operator} {ri.Type()}");
            }
        }

        private IObject EvalPrefixExpression(string @operator, IObject right)
        {
            switch (@operator)
            {
                case "!":
                    return EvalBangOperatorExpression(right);
                case "-":
                    return EvalMinusPrefixOperatorExpression(right);
                default:
                    return NewError($"unkown operator: {@operator} {right.Type()}");
            }
        }

        private IObject EvalMinusPrefixOperatorExpression(IObject right)
        {
            if (right is Integer integer)
            {
                var value = integer.Value;
                return new Integer { Value = -value };
            }
            else
            {
                return NewError($"unknown operator: -{right.Type()}");
            }
        }

        private IObject EvalBangOperatorExpression(IObject right)
        {
            if (right == Obj.Boolean.True)
            {
                return Obj.Boolean.False;
            }
            else if (right == Obj.Boolean.False)
            {
                return Obj.Boolean.True;
            }
            else if (right == Obj.Null.Instance)
            {
                return Obj.Boolean.True;
            }
            else
            {
                return Obj.Boolean.False;
            }
        }

        private IObject EvalProgram(Program program, Environment env)
        {
            IObject result = default(IObject);
            foreach (var stmt in program.Statements)
            {
                result = Eval(stmt, env);

                if (result is ReturnValue rv)
                {
                    return rv.Value;
                }
                else if (result is Error err)
                {
                    return err;
                }
            }

            return result;
        }

        private Error NewError(string message)
        {
            return new Error { Message = message };
        }

        private bool IsError(IObject obj)
        {
            return obj != null && obj.Type() == ObjectType.Error;
        }
    }
}
