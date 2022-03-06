using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

//interface
public interface KeyCodeInfo
{
    public KeyCode code { get; }
    float time { get; }
    public bool chacked(in float time);
}
//キー押下用(new)
public readonly struct KeyDowns : KeyCodeInfo
{
    public readonly KeyCode code { get; }
    public readonly float time { get; }

    public KeyDowns(in KeyCode code, in float time)
    {
        this.code = code;
        this.time = time;
    }
    public readonly bool chacked(in float time) => Input.GetKeyDown(code) && time < this.time;
}
//キー離上用(new)
public readonly struct KeyUps : KeyCodeInfo
{
    public readonly KeyCode code { get; }
    public readonly float time { get; }

    public KeyUps(in KeyCode code, in float time)
    {
        this.code = code;
        this.time = time;
    }
    public readonly bool chacked(in float time) => Input.GetKeyUp(code) && time > this.time;
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
            //時間を進める
            time += Time.deltaTime;

            bool Prevkey = newkey;
            newkey = Input.anyKey;

            bool reset = false;

            //押下時の判定
            if ((!Prevkey && newkey) && keyCodeInfos[ComboCount] is KeyDowns)
            {
                if (!keyCodeInfos[ComboCount++].chacked(time))
                {
                    ComboCount = 0;
                }
                else
                    Processing(ComboCount);

                time = 0.0f;
            }
            //離上時の判定
            else if ((Prevkey && !newkey) && keyCodeInfos[ComboCount] is KeyUps)
            {
                if (!keyCodeInfos[ComboCount++].chacked(time))
                {
                    ComboCount = 0;
                }
                else
                    Processing(ComboCount);

                time = 0.0f;
            }

            //入力タイムアウトでリセット
            if (!newkey && (time > timeout))
                reset = true;

            //一致したら処理でリセット
            if (keyCodeInfos.Length == ComboCount)
            {
                await UniTask.Run(OnComplete, false, token);
                reset = true;
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

        //文字列系はメモリを消費しやすいので注意
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(typeing.Length);

        while (!token.IsCancellationRequested)
        {
            if (Input.anyKeyDown)
            {
                stringBuilder.Append(Input.inputString);

                //外したらクリア(一つ戻す等に書き換えても可)
                if (typeing.IndexOf(stringBuilder.ToString()) != 0)
                    stringBuilder.Clear();
                //一致中
                else if(stringBuilder.Length > 0)
                    Processing(stringBuilder.ToString());

                //一致したら処理
                if (typeing == stringBuilder.ToString())
                    await UniTask.Run(OnComplete, false, token);
            }

            //待つ
            await UniTask.Yield();
        }
    }
}