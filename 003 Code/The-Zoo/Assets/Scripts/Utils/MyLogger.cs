using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utils
{
    public static class MyLogger
    {
        /// <summary>
        /// 클래스명과 메서드명을 함께 남기는 로그 함수
        /// </summary>
        public static void Print(
            object instance = null,
            string message = "",
            [CallerMemberName] string memberName = "")
        {
            var className = instance != null
                ? instance.GetType().Name
                : "<static>";

            var logMsg = $"[{className}::{memberName}] {message}";
            Debug.Log(logMsg);
        }

        /// <summary>
        /// 호출된 위치의 전체 네임스페이스.클래스.메서드명을 남기는 함수 (스택트레이스)
        /// </summary>
        public static void Trace(
            string message = "")
        {
            var stack = new System.Diagnostics.StackTrace();
            var frame = stack.GetFrame(1);
            var method = frame.GetMethod();
            var fullName = $"{method.DeclaringType.FullName}::{method.Name}";
            Debug.Log($"[{fullName}] {message}");
        }
    }
}