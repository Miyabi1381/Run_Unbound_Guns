using Tanks.Complete;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootStrap : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private void Awake()
    {
        // Bootstrapシーンから起動したかを、DontDestroyOnLoadで引っ越す前に覚えておく（後の「保険」の節で使う）
        bool startedFromBootstrap = gameObject.scene.name == "MarinaTestScene";

        DontDestroyOnLoad(gameObject); // 常駐化はここ1回だけ

        // 初期化順を、ここに1回だけ・上から順に書く
       

        if (startedFromBootstrap) SceneManager.LoadScene("TitleScene"); // 組み立て完了、タイトルへ
    }
}
