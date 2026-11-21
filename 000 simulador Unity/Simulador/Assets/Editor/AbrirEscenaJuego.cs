// Si Unity abre una escena vacia ("Untitled", sin archivo), carga sola la escena del simulador (SampleScene).
// Paso el 19/11: despues de compilar en segundo plano, Unity quedo en "Untitled" y al dar Play solo aparecian los ojitos.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AbrirEscenaJuego
{
    const string ESCENA = "Assets/Scenes/SampleScene.unity";
    static AbrirEscenaJuego() { EditorApplication.delayCall += Revisar; }
    static void Revisar()
    {
        if (UnityEngine.Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var s = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(s.path) && System.IO.File.Exists(ESCENA)) EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
    }
}
