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
//�L�[�����p(new)
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
//�L�[����p(new)
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
    /// �R���X�g���N�^
    /// InputCmd inputCmd = new InputCmd(new KeyDowns(KeyCode, timeout),...
    /// </summary>
    /// <param name="keyCodeInfos"></param>
    public InputCmd(params KeyCodeInfo[] keyCodeInfos)
    {
        this.keyCodeInfos = keyCodeInfos;
        tokenSource = new CancellationTokenSource();
    }
    /// <summary>
    /// �ݒ肵���R�}���h�`�F�b�N�i�f�o�b�O�p�j
    /// inputCmd.AddListener(3.0f, (s) => { }, () => { });
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="Processing"></param>
    /// <param name="OnComplete"></param>
    public void AddListener(in float timeout, in Action<int> Processing, in Action OnComplete) => CmdCheck(OnComplete, Processing, timeout, tokenSource.Token).Forget();
    /// <summary>
    /// �ݒ肵����������̓`�F�b�N�i�f�o�b�O�p�j
    /// inputCmd.AddListener("", (s) => { }, () => { });
    /// </summary>
    /// <param name="typeing"></param>
    /// <param name="Processing"></param>
    /// <param name="OnComplete"></param>
    public void AddListener(in string typeing, in Action<string> Processing, in Action OnComplete) => TypeingCheck(OnComplete, Processing, typeing, tokenSource.Token).Forget();
    /// <summary>
    /// UniTask��~
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
            //���Ԃ�i�߂�
            time += Time.deltaTime;

            bool Prevkey = newkey;
            newkey = Input.anyKey;

            bool reset = false;

            //�������̔���
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
            //���㎞�̔���
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

            //���̓^�C���A�E�g�Ń��Z�b�g
            if (!newkey && (time > timeout))
                reset = true;

            //��v�����珈���Ń��Z�b�g
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

            //�҂�
            await UniTask.Yield();
        }
    }

    private async UniTask TypeingCheck(Action OnComplete, Action<string> Processing, string typeing, CancellationToken token)
    {
        if (OnComplete == null || Processing == null)
            return;

        //������n�̓�����������₷���̂Œ���
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(typeing.Length);

        while (!token.IsCancellationRequested)
        {
            if (Input.anyKeyDown)
            {
                stringBuilder.Append(Input.inputString);

                //�O������N���A(��߂����ɏ��������Ă���)
                if (typeing.IndexOf(stringBuilder.ToString()) != 0)
                    stringBuilder.Clear();
                //��v��
                else if(stringBuilder.Length > 0)
                    Processing(stringBuilder.ToString());

                //��v�����珈��
                if (typeing == stringBuilder.ToString())
                    await UniTask.Run(OnComplete, false, token);
            }

            //�҂�
            await UniTask.Yield();
        }
    }
}