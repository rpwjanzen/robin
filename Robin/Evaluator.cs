namespace Robin.Eval
{
    using System;
    using Robin.Ast;
    using Robin.Obj;
    using Environment = Robin.Environment;

    class Evaluator
    {
        System.Collections.Generic.Dictionary<string, Obj.Builtin> builtins = new System.Collections.Generic.Dictionary<string, Builtin>
        {
            {
                "len", new Builtin {
                    Fn = (IObject[] args) =>
                    {
                        if (args.Length != 1)
                        {
                            return NewError($"wrong number of arguments. got={args.Length}, want=1");
                        }

                        if (args[0] is Obj.String s)
                        {
                            return new Integer { Value = s.Value.Length };
                        } else if (args[0] is Obj.Array arr)
                        {
                            return new Integer { Value = arr.Elements.LongLength };
                        }

                        return NewError($"argument to `len` not supported, got {args[0].Type()}");
                    }
                }
            },
            {
                "first", new Builtin
                {
                    Fn = (IObject[] args) =>
                    {
                        if (args.Length != 1)
                        {
                            return NewError($"wrong number of arguments. got={args.Length}, want=1");
                        }

                        if (args[0].Type() != ObjectType.Array)
                        {
                            return NewError($"argument to `first` must be ARRAY, got {args[0].Type()}");
                        }

                        var arr = (Obj.Array)args[0];
                        if (arr.Elements.Length > 0)
                        {
                            return arr.Elements[0];
                        }

                        return Null.Instance
                    }
                }
            },
            {
                "last", new Builtin
                {
                    Fn = (IObject[] args) =>
                    {
                        if (args.Length != 1)
                        {
                            return NewError($"wrong number of arguments, got={args.Length}, want=1");
                        }

                        if (args[0].Type() != ObjectType.Array)
                        {
                            return NewError($"argument to `last` must be ARRAY, got {args[0].Type()}");
                        }

                        var arr = (Obj.Array)args[0];
                        var length = arr.Elements.LongLength;
                        if (length > 0)
                        {
                            return arr.Elements[length - 1];
                        }

                        return Null.Instance;
                    }
                }
            },
            {
                "rest", new Builtin
                {
                    Fn = (IObject[] args) =>
                    {
                        if (args.Length != 1)
                        {
                            return NewError($"wrong number of arguments, got={args.Length}, want=1");
                        }

                        if (args[0].Type() != ObjectType.Array)
                        {
                            return NewError($"argument to `rest` must be ARRAY, got {args[0].Type()}");
                        }

                        var arr = (Obj.Array)args[0];
                        var length = arr.Elements.LongLength;
                        if (length > 0)
                        {
                            var newElements = new IObject[length - 1];
                            System.Array.Copy(arr.Elements, 1, newElements, 0, length - 1);
                            return new Obj.Array { Elements = newElements };
                        }

                        return Obj.Null.Instance;
                    }
                }
            },
            {
                "push", new Builtin
                {
                    Fn = (IObject[] args) =>
                    {
                        if (args.Length != 2)
                        {
                            return NewError($"wrong number of arguments, got={args.Length}, want=2");
                        }

                        if (args[0].Type() != ObjectType.Array)
                        {
                            return NewError($"argument to `push` must be ARRAY, got {args[0].Type()}");
                        }

                        var arr = (Obj.Array)args[0];
                        var length = arr.Elements.Length;
                        var newElements = new IObject[length + 1];
                        System.Array.Copy(arr.Elements, 0, newElements, 0, length);
                        newElements[length] = args[1];
                        return new Obj.Array { Elements = newElements };
                    }
                }
            },
            {
                "puts", new Builtin
                {
                    Fn = (IObject[] args) =>
                    {
                        foreach (var arg in args)
                        {
                            Console.WriteLine(arg.Inspect());
                        }

                        return Obj.Null.Instance;
                    }
                }
            }
        };

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
                // return value that is assigned to let expression
                return val;
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

                if (fn is Function || fn is Builtin)
                {
                    return AppyFunction(fn, args);
                }

                return NewError($"not a function: {fn.Type()}");
            }
            else if (node is StringLiteral s)
            {
                return new Obj.String { Value = s.Value };
            }
            else if (node is ArrayLiteral arr)
            {
                var elements = EvalExpressions(arr.Elements, env);
                if (elements.Length == 1 && IsError(elements[0]))
                {
                    return elements[0];
                }
                return new Obj.Array { Elements = elements };
            }
            else if (node is IndexExpression iexpr)
            {
                var left = Eval(iexpr.Left, env);
                if (IsError(left))
                {
                    return left;
                }
                var index = Eval(iexpr.Index, env);
                if (IsError(index))
                {
                    return index;
                }

                return EvalIndexExpression(left, index);
            }
            else if (node is HashLiteral hl)
            {
                return EvalHashLiteral(hl, env);
            }
            else
            {
                return Obj.Null.Instance;
            }
        }

        private IObject EvalHashLiteral(HashLiteral hl, Environment env)
        {
            var pairs = new System.Collections.Generic.Dictionary<Obj.HashKey, Obj.HashPair>();
            foreach (var kvp in hl.Pairs)
            {
                var key = Eval(kvp.Key, env);
                if (IsError(key))
                {
                    return key;
                }

                var hashKey = key as Hashable;
                if (hashKey == null)
                {
                    return NewError($"unusable as hash key: {key.Type()}");
                }

                var value = Eval(kvp.Value, env);
                if (IsError(value))
                {
                    return value;
                }

                var hashed = hashKey.HashKey();
                pairs[hashed] = new HashPair { Key = key, Value = value };
            }

            return new Obj.Hash { Pairs = pairs };
        }

        private IObject EvalIndexExpression(IObject left, IObject index)
        {
            if (left.Type() == ObjectType.Array && index.Type() == ObjectType.Int)
            {
                return EvalArrayIndexExpression((Obj.Array)left, (Integer)index);
            } else if (left.Type() == ObjectType.Hash)
            {
                return EvalHashExpression(left, index);
            }
            return NewError($"index operator not supported: {left.Type()}");
        }

        private IObject EvalHashExpression(IObject hash, IObject index)
        {
            var hashObject = (Hash)hash;
            var key = index as Hashable;
            if (key == null)
            {
                return NewError($"unusable as hash key: {index.Type()}");
            }

            if (!hashObject.Pairs.TryGetValue(key.HashKey(), out HashPair pair))
            {
                return Null.Instance;
            }

            return pair.Value;
        }

        private IObject EvalArrayIndexExpression(Obj.Array left, Integer index)
        {
            var idx = index.Value;
            var max = left.Elements.Length - 1;

            if (idx < 0 || idx > max)
            {
                return Obj.Null.Instance;
            }

            return left.Elements[idx];
        }

        private IObject AppyFunction(IObject fn, IObject[] args)
        {
            if (fn is Function f)
            {
                var extendedEnv = ExtendFunctionEnv(f, args);
                var evaluated = Eval(f.Body, extendedEnv);
                return UnwrapReturnValue(evaluated);
            }

            if (fn is Builtin b)
            {
                return b.Fn(args);
            }

            return NewError($"not a function: {fn.Type()}");
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

            if (builtins.TryGetValue(id.Value, out Builtin b))
            {
                return b;
            }

            return NewError($"identifier not found: {id.Value}");
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

            if (left is Obj.String ls && right is Obj.String rs)
            {
                return EvalStringInfixExpression(@operator, ls, rs);
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

        private IObject EvalStringInfixExpression(string @operator, Obj.String ls, Obj.String rs)
        {
            if (@operator == "+")
            {
                var leftVal = ls.Value;
                var rightVal = rs.Value;
                return new Obj.String { Value = leftVal + rightVal };
            }

            return NewError($"unknown operator: {ls.Type()} {@operator} {rs.Type()}");
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

        private static Error NewError(string message)
        {
            return new Error { Message = message };
        }

        private bool IsError(IObject obj)
        {
            return obj != null && obj.Type() == ObjectType.Error;
        }
    }
}
