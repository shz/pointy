using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq.Expressions;

namespace Pointy.Routers
{
    using Handler = Func<HTTP.Request, HTTP.Response, Task>;
    using ParamHandler = Func<IDictionary<string, string>, HTTP.Request, HTTP.Response, Task>;

    class MethodNotAllowedException : Exception
    {
        public MethodNotAllowedException()
            : base("Classes may not route based on method")
        {

        }
    }
    class InvalidConstructorException : Exception
    {
        public InvalidConstructorException(string message)
            : base(message)
        { }
    }
    class BadMethodSignatureException : Exception
    {
        public BadMethodSignatureException(string reason)
            : base(reason)
        {

        }
    }

    class RouteBuilder
    {
        /// <summary>
        /// Caller is reponsible for ensuring that the params passed to the handler fulfill the
        /// method's parameters.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static Func<IDictionary<string, string>, HTTP.Request, HTTP.Response, Task> Build(Type t, MethodInfo method)
        {
            // The constructor we'll use
            ConstructorInfo constructor = null;
            // Constructor parameters we'll use
            ParameterInfo[] constructorParams = null;
            // Class Request field
            FieldInfo reqField = null;
            // Class response field
            FieldInfo resField = null;

            // Request/response parameters from the lambda
            var parmsParam = Expression.Parameter(typeof(IDictionary<string, string>), "params");
            var reqParam = Expression.Parameter(typeof(HTTP.Request), "request");
            var resParam = Expression.Parameter(typeof(HTTP.Response), "response");

            // Collect constructor data
            var constrs = t.GetConstructors();
            if (constrs.Length == 0)
                throw new InvalidConstructorException("Handler classes must define a constructor");
            if (constrs.Length > 1)
                throw new InvalidConstructorException("Handler classes may only define one constructor");
            constructorParams = constrs[0].GetParameters();
            if (constructorParams.Length == 0)
                constructor = constrs[0];
            else if (constructorParams.Length == 2 && constructorParams[0].ParameterType == typeof(HTTP.Request)
            && constructorParams[1].ParameterType == typeof(HTTP.Response))
                constructor = constrs[0];
            if (constructor == null)
                throw new InvalidConstructorException("No valid constructor signature found");

            // Check for class request/response fields
            foreach (var field in t.GetFields())
                if (field.FieldType == typeof(HTTP.Request))
                    reqField = field;
                else if (field.FieldType == typeof(HTTP.Response))
                    resField = field;

            // Build the lambda

            var steps = new List<Expression>();
            var objVariable = Expression.Variable(t, "handler");
            steps.Add(objVariable);

            // Create the object
            steps.Add(Expression.Assign(objVariable, constructorParams.Length == 0
                ? Expression.New(constructor)
                : Expression.New(constructor, reqParam, resParam)
            ));

            // If the class has public request/response field, assign to them
            if (reqField != null)
                steps.Add(Expression.Assign(Expression.Field(objVariable, reqField), reqParam));
            if (resField != null)
                steps.Add(Expression.Assign(Expression.Field(objVariable, resField), resParam));

            // Call the method, passing in possibly req/res, and possibly params
            var callParams = new List<Expression>();
            var containsKey = typeof(IDictionary<string, string>).GetMethod("ContainsKey");
            Func<ParameterInfo, Expression> makeGetter = delegate(ParameterInfo param)
            {
                MethodInfo convert = null;
                var strArray = new Type[] { typeof(string) };
                if (param.ParameterType == typeof(int))
                    convert = typeof(Int32).GetMethod("Parse", strArray);
                else if (param.ParameterType == typeof(long))
                    convert = typeof(Int64).GetMethod("Parse", strArray);
                else if (param.ParameterType == typeof(float))
                    convert = typeof(Single).GetMethod("Parse", strArray);
                else if (param.ParameterType == typeof(double))
                    convert = typeof(Double).GetMethod("Parse", strArray);
                else if (param.ParameterType == typeof(decimal))
                    convert = typeof(Decimal).GetMethod("Parse", strArray);

                var getExpr = Expression.Property(parmsParam, "Item", Expression.Constant(param.Name));

                return Expression.Condition(
                    // If
                    Expression.Call(parmsParam, containsKey, Expression.Constant(param.Name)),
                    // Then
                    convert == null ? (Expression)getExpr : (Expression)Expression.Call(convert, getExpr),
                    // Else
                    Expression.Default(param.ParameterType)
                );
            };

            foreach (var param in method.GetParameters())
                if (param.ParameterType == typeof(HTTP.Request))
                    callParams.Add(reqParam);
                else if (param.ParameterType == typeof(HTTP.Response))
                    callParams.Add(resParam);
                else
                    callParams.Add(makeGetter(param));

            steps.Add(Expression.Call(objVariable, method, callParams));

            // Wrap up
            var lambda = (Func<IDictionary<string, string>, HTTP.Request, HTTP.Response, Task>)Expression.Lambda(
                Expression.Block(typeof(Task), new ParameterExpression[] { objVariable }, steps),
                parmsParam,
                reqParam,
                resParam
            ).Compile();
            return lambda;
        }
    }

    /// <summary>
    /// Routes requests based on URL
    /// </summary>
    public class URLRouter : List<Tuple<string, string, ParamHandler>>, IRouter
    {
        Dictionary<string, Regex> Regexes = new Dictionary<string, Regex>();

        public ParamHandler Default { get; set; }
        public void Add(string path, string method, ParamHandler handler, string name = null)
        {
            this.Add(new Tuple<string, string, ParamHandler>(path, method, handler));
        }
        public void Add(string path, ParamHandler handler, string name = null)
        {
            this.Add(path, null, handler, name);
        }

        public Handler Resolve(HTTP.Request request)
        {
            foreach (var route in this)
            {
                // Skip if the methods don't match
                if (route.Item2 != null && !String.Equals(route.Item2, request.Method, StringComparison.OrdinalIgnoreCase))
                    continue;

                Regex r = null;
                if (!Regexes.TryGetValue(route.Item1, out r))
                {
                    lock (Regexes)
                    {
                        if (!Regexes.TryGetValue(route.Item1, out r))
                        {
                            // Build the regex for this path
                            var re = Regex.Replace(route.Item1, @"{([a-z_][a-z0-9_]*)( (.*?))?}", m =>
                            {
                                var pattern = @"[\w\.-]+";
                                if (m.Groups[3].Success)
                                    pattern = m.Groups[3].Value;
                                return String.Format(@"(?<{0}>{1})", m.Groups[1].Value, pattern);
                            }, RegexOptions.IgnoreCase);
                            r = new Regex("^" + re + "$", RegexOptions.Compiled);

                            Regexes[route.Item1] = r;
                        }
                    }
                }

                // If the regex matches the path, we have a match!
                var match = r.Match(request.Path);
                if (match.Success)
                {
                    var parms = new Dictionary<string, string>();
                    foreach (var g in r.GetGroupNames())
                        parms[g] = match.Groups[g].Value;
                    // Bah, WTB currying
                    return (req, res) => route.Item3(parms, req, res);
                }
            }
            if (Default != null)
                return (req, res) => Default(new Dictionary<string, string>(), req, res);
            else
                return null;
        }
        public static URLRouter Discover()
        {
            var router = new URLRouter();

            // Fetch all types from the *calling* assembly.  This
            // should allow us to pick up all the user's classes, but
            // exclude anything in a third-party library that may
            // have route attributes.
            var types = Assembly.GetCallingAssembly().GetTypes();
            foreach (var t in types.Where(t => t.IsClass))
            {
                var attribute = t.GetCustomAttribute<RouteAttribute>(false);
                if (attribute == null)
                    continue;

                // If the attribute has a method, throw an exception because
                // it's not allowed.
                if (attribute.Method != null)
                    throw new MethodNotAllowedException();

                // Find the base path to work with
                var basePath = attribute.Path;

                // Add a route for each public method with the route attribute
                foreach (var m in t.GetMethods())
                {
                    attribute = m.GetCustomAttribute<RouteAttribute>(false);
                    if (attribute == null)
                        continue;

                    // May need to tweak this
                    var methodPath = attribute.Path;

                    // Make sure the method returns a Task
                    if (m.ReturnType != typeof(Task))
                        throw new BadMethodSignatureException(String.Format("Routed method {0}.{1}.{2} must return a Task", t.Namespace, t.Name, m.Name));

                    // Parse out the params and compare them 
                    var parms = new Dictionary<string, bool>();
                    foreach (Match match in Regex.Matches(attribute.Path, @"{([a-z_][a-z0-9_]*)(.*)}"))
                        parms[match.Groups[1].Value] = match.Groups[2].Success && match.Groups[2].Length > 1;
                    foreach (var param in m.GetParameters())
                    {
                        if (param.ParameterType == typeof(HTTP.Request) || param.ParameterType == typeof(HTTP.Response))
                            continue;

                        if (param.ParameterType != typeof(string))
                        {
                            var parmCustomRegex = false;
                            if (!parms.TryGetValue(param.Name, out parmCustomRegex))
                                throw new BadMethodSignatureException(String.Format("Routed method {0}.{1}.{2} has parameter '{3}' but no corresponding parameter in path", t.Namespace, t.Name, m.Name, param.Name));
                            if (parmCustomRegex)
                                throw new BadMethodSignatureException(String.Format("Routed method {0}.{1}.{2} has parameter '{3}' of type '{4}' with custom regex in path string.  Custom parameter regexes are only supported with Strings", t.Namespace, t.Name, m.Name, param.Name, param.ParameterType.Name));
                        }
                        if (param.ParameterType == typeof(float) || param.ParameterType == typeof(double) || param.ParameterType == typeof(decimal))
                            methodPath = methodPath.Replace(String.Format("{{0}}", param.Name), String.Format(@"{{0}} \d+(\.\d+)?", param.Name));
                        else if (param.ParameterType == typeof(int) || param.ParameterType == typeof(long))
                            methodPath = methodPath.Replace(String.Format("{{0}}", param.Name), String.Format(@"{{0}} \d+", param.Name));
                        else if (param.ParameterType != typeof(string))
                            throw new BadMethodSignatureException(String.Format("Routed method {0}.{1}.{2} has parameter '{3}' of type '{4}'.  Don't know how to parse '{4}'", t.Namespace, t.Name, m.Name, param.Name, param.ParameterType.Name));
                    }
                    // Only allow public methods
                    if (!m.IsPublic)
                        throw new BadMethodSignatureException("Routed method must be public");

                    // Attempt to build the route
                    var lambda = RouteBuilder.Build(t, m);

                    // Add the route
                    if (attribute.Path == "404")
                        router.Default = lambda;
                    else
                        router.Add(basePath + methodPath, attribute.Method, lambda, null);
                }
            }

            return router;
        }
    }

    #region Attributes

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RouteAttribute : Attribute
    {
        public string Path
        {
            get;
            private set;
        }
        public string Method
        {
            get;
            set;
        }

        public RouteAttribute(string path)
        {
            if (path == null)
                path = "";
            Path = path;
        }
    }

    // Helper routes for common methods

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class GetAttribute : RouteAttribute
    {
        public GetAttribute(string path)
            : base(path)
        {
            Method = "GET";
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PostAttribute : RouteAttribute
    {
        public PostAttribute(string path)
            : base(path)
        {
            Method = "POST";
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PutAttribute : RouteAttribute
    {
        public PutAttribute(string path)
            : base(path)
        {
            Method = "PUT";
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DeleteAttribute : RouteAttribute
    {
        public DeleteAttribute(string path)
            : base(path)
        {
            Method = "DELETE";
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DefaultAttribute : RouteAttribute
    {
        public DefaultAttribute()
            : base("404")
        {

        }
    }

    #endregion

}
