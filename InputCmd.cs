using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

//interface
public interface KeyCodeInfo
{
    float time { get; }
    public bool chacked(in float time);
}
//複数キー押下用(new)
public readonly struct MulKeyDowns : KeyCodeInfo
{
    public readonly float time { get; }
    public readonly KeyCode[] code { get; }

    public MulKeyDowns(in float time, params KeyCode[] code)
    {
        this.time = time;
        this.code = code;
    }
    public readonly bool allchacked()
    {
        for (int i = 0; i < code.Length; i++)
        {
            if (!Input.GetKey(code[i]))
                return false;
        }
        for (int i = 0; i < code.Length; i++)
        {
            if (Input.GetKeyDown(code[i]))
                return true;
        }
        return false;
    }
    public readonly bool chacked(in float time) => time < this.time && allchacked();
}
//キー押下用(new)
public readonly struct KeyDowns : KeyCodeInfo
{
    public readonly float time { get; }
    public readonly KeyCode code { get; }

    public KeyDowns(in float time, in KeyCode code)
    {
        this.time = time;
        this.code = code;
    }
    public readonly bool chacked(in float time) => time < this.time && Input.GetKeyDown(code);
}
//キー離上用(new)
public readonly struct KeyUps : KeyCodeInfo
{
    public readonly float time { get; }
    public readonly KeyCode code { get; }

    public KeyUps(in float time, in KeyCode code)
    {
        this.time = time;
        this.code = code;
    }
    public readonly bool chacked(in float time) => time > this.time && Input.GetKeyUp(code);
}
public class InputCmd
{
    public readonly KeyCodeInfo[] keyCodeInfos;
    private CancellationTokenSource tokenSource;
    /// <summary>
    /// コンストラクタ
    /// InputCmd inputCmd = new InputCmd(new KeyDowns(KeyCode, timeout),...
    /// </summary>
    /// <param name="keyCodeInfos"></param>
    public InputCmd(params KeyCodeInfo[] keyCodeInfos)
    {
        this.keyCodeInfos = keyCodeInfos;
        tokenSource = new CancellationTokenSource();
    }
    /// <summary>
    /// 設定したコマンドチェック（デバッグ用）
    /// inputCmd.AddListener(3.0f, (s) => { }, () => { });
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="Processing"></param>
    /// <param name="OnComplete"></param>
    public void AddListener(in float timeout, in Action<int> Processing, in Action OnComplete) => CmdCheck(OnComplete, Processing, timeout, tokenSource.Token).Forget();
    /// <summary>
    /// 設定した文字列入力チェック（デバッグ用）
    /// inputCmd.AddListener("", (s) => { }, () => { });
    /// </summary>
    /// <param name="typeing"></param>
    /// <param name="Processing"></param>
    /// <param name="OnComplete"></param>
    public void AddListener(in string typeing, in Action<string> Processing, in Action OnComplete) => TypeingCheck(OnComplete, Processing, typeing, tokenSource.Token).Forget();
    /// <summary>
    /// UniTask停止
    /// </summary>
    public void RemoveListener() => tokenSource.Cancel();
    private async UniTask CmdCheck(Action OnComplete, Action<int> Processing, float timeout, CancellationToken token)
    {
        float time = 0f;
        int ComboCount = 0;
        bool newkey = false;

        if (OnComplete == null || Processing == null)
            return;

        while (!token.IsCancellationRequested)
        {
            bool chack = false;
            bool Prevkey = newkey; 
            newkey = Input.anyKey;
            //押下中の判定
            if (newkey && keyCodeInfos[ComboCount] is MulKeyDowns) chack = true;
            //押下時の判定
            else if ((!Prevkey && newkey) && keyCodeInfos[ComboCount] is KeyDowns) chack = true;
            //離上時の判定
            else if ((Prevkey && !newkey) && keyCodeInfos[ComboCount] is KeyUps) chack = true;

            //時間を進める
            time += Time.deltaTime;
            //入力タイムアウトでリセット
            bool reset = !newkey && (time > timeout);

            if (chack)
            {
                if (!keyCodeInfos[ComboCount++].chacked(time))
                    ComboCount = 0;
                else
                    Processing(ComboCount);

                time = 0.0f;

                //一致したら処理でリセット
                if (keyCodeInfos.Length == ComboCount)
                {
                    await UniTask.Run(OnComplete, false, token);
                    reset = true;
                }
            }

            if (reset)
            {
                ComboCount = 0;
                time = 0.0f;
            }

            //待つ
            await UniTask.Yield();
        }
    }

    private async UniTask TypeingCheck(Action OnComplete, Action<string> Processing, string typeing, CancellationToken token)
    {
        if (OnComplete == null || Processing == null)
            return;

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(typeing.Length);

        while (!token.IsCancellationRequested)
        {
            //入力中
            if (Input.anyKeyDown && stringBuilder.Append(Input.inputString).Length > 0)
            {
                //不一致
                if (typeing.IndexOf(stringBuilder.ToString()) != 0)
                    stringBuilder.Clear();

                //一致中
                else if (stringBuilder.Length != typeing.Length)
                    Processing(stringBuilder.ToString());

                //完全一致
                else
                {
                    await UniTask.Run(OnComplete, false, token);
                    break;
                }
            }
            //待つ
            await UniTask.Yield();
        }
    }
}