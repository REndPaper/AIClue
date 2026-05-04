using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class PreLoader
{
    // ==========================================
    // 🐧 리눅스 전용 커널 API (dlopen)
    // ==========================================
    private const int RTLD_NOW = 2;
    private const int RTLD_GLOBAL = 0x0100;

    [DllImport("libdl.so.2", CharSet = CharSet.Ansi)]
    private static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so.2", CharSet = CharSet.Ansi)]
    private static extern IntPtr dlerror();

    // 유니티 시스템 초기화 전, 가장 먼저 무조건 실행됨
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void PreloadLibraries()
    {
        Debug.Log("================= [통신 정비] 크로스 플랫폼 강제 결속 시작 =================");
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        // 🐧 리눅스 우분투 환경 로직
        string linuxPluginDir = Path.Combine(Application.dataPath, "Plugins/Linux/x86_64"); // 리눅스 폴더 경로
        string[] linuxLibs = { "libggml-base.so", "libggml.so", "libggml-vulkan.so", "libllama.so" };

        foreach (var lib in linuxLibs)
        {
            string fullPath = Path.Combine(linuxPluginDir, lib);
            if (File.Exists(fullPath))
            {
                // RTLD_GLOBAL을 줘서 다른 파일들이 이 라이브러리를 전역에서 찾을 수 있게 함
                IntPtr handle = dlopen(fullPath, RTLD_NOW | RTLD_GLOBAL);
                if (handle == IntPtr.Zero)
                {
                    IntPtr errPtr = dlerror();
                    string error = Marshal.PtrToStringAnsi(errPtr);
                    Debug.LogError($"[리눅스 OS] {lib} 적재 실패! 진짜 원인: {error}");
                }
                else
                {
                    Debug.Log($"[리눅스 OS] {lib} 적재 완료! (메모리 주소: {handle})");
                }
            }
            else
            {
                Debug.LogError($"[리눅스 OS] 파일을 찾을 수 없습니다: {fullPath}");
            }
        }
#endif
        Debug.Log("============================================================================");
    }
}