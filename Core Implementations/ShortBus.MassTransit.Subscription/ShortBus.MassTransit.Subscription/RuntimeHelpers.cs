using System;
using System.Reflection;
using Magnum.StateMachine;
using MassTransit.Saga;

namespace ShortBus.MassTransitHelper.Subscription
{
    internal static class RuntimeHelpers
    {
        //public static object RunInstanceMethod(System.Type t, string strMethod,
        //                                       object objInstance, object[] aobjParams)
        //{
        //    BindingFlags eFlags = BindingFlags.Instance | BindingFlags.Public |
        //                          BindingFlags.NonPublic;
        //    return RunMethod(t, strMethod,
        //                     objInstance, aobjParams, eFlags);
        //}

        //private static object RunMethod(System.Type t, string
        //                                                   strMethod, object objInstance, object[] aobjParams, BindingFlags eFlags)
        //{
        //    MethodInfo m;
        //    try
        //    {
        //        m = t.GetMethod(strMethod, eFlags);
        //        if (m == null)
        //        {
        //            throw new ArgumentException("There is no method '" +
        //                                        strMethod + "' for type '" + t.ToString() + "'.");
        //        }

        //        object objRet = m.Invoke(objInstance, aobjParams);
        //        return objRet;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public static void SetCurrentState<TSaga>(this TSaga saga, State state)
            where TSaga : SagaStateMachine<TSaga>
        {
            var newState = State<TSaga>.GetState(state);

            FieldInfo field = typeof(StateMachine<TSaga>)
                .GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            field.SetValue(saga, newState);
        }
    }
}