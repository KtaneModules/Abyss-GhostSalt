using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class AbyssScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public TextMesh Seed, ReadyText, LastInput;
    public KMSelectable ReadyButton, Orb;
    public Material TextMaterial, HighlightMat, PostProcessMaterial;
    public GameObject OrbHighlight;
    public SpriteRenderer Light;

    private KMAudio.KMAudioRef Sound;

    private Transform MainCameraTransform = null;
    private Coroutine RunningCoroutine;
    private int[] Numbers = new int[8];
    private int ReadyPresses, PrevPresses, ForePresses;
    private string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private string LetterList = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private string NumberList = "0123456789";
    private string SeedVar, NumbersConcat;
    private string Input = "";
    private bool PressedReady, PlayingSound, Highlighting, OverOneSecond, Running, Solved, Active, DisableFilter;
    private CameraPostProcess PostProcess = null;

    private bool LetterLess(char Character)
    {
        if (NumberList.IndexOf(Character) == -1)
        {
            if (LetterList.IndexOf(Character) < 14)
                return true;
        }
        else
            if (int.Parse(Character.ToString()) < 5)
            return true;
        return false;
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        Orb.transform.localScale = new Vector3(0f, 0f, 0f);
        MainCameraTransform = Camera.main.transform;
        Seed.text = "";
        ReadyText.text = "";
        LastInput.text = "";
        ReadyButton.transform.localScale = new Vector3(0, 0, 0);
        Light.color = new Color(Light.color.r, Light.color.g, Light.color.b, 0);
        TextMaterial.mainTextureOffset = new Vector2(0, 0);
        TextMaterial.mainTextureScale = new Vector2(1, 1);
        for (int i = 0; i < 16; i++)
            SeedVar += Alphabet[Rnd.Range(0, Alphabet.Length)];
        Module.OnActivate += delegate { Seed.text = SeedVar.Substring(0, 8) + "\n" + SeedVar.Substring(8, 8); ReadyText.text = "Ready"; ReadyButton.transform.localScale = new Vector3(1, 1, 1); Active = true; Calculate(); };
        ReadyButton.OnHighlight += delegate { ReadyButton.GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 255, 192); };
        ReadyButton.OnHighlightEnded += delegate { ReadyButton.GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 255, 64); };
        ReadyButton.OnInteract += delegate { if (!PressedReady && Active) { StartCoroutine(ReadyPress()); PressedReady = true; } return false; };
        Orb.OnHighlight += delegate { if (!Solved) { Highlighting = true; StartCoroutine(OrbHighlighted()); } };
        Orb.OnHighlightEnded += delegate { if (!Solved) { if (OverOneSecond) StartCoroutine(CloseLight()); CalcInput(); Highlighting = false; OverOneSecond = false; } };
        Orb.OnInteract += delegate { if (!Solved) OrbPress(); return false; };
        Bomb.OnBombExploded += delegate { if (PlayingSound) Sound.StopSound(); };
        StartCoroutine(OrbAnim());
    }

    void Calculate()
    {
        Debug.LogFormat("[Abyss #{0}] The seed is {1}.", _moduleID, SeedVar);

        for (int i = 0; i < 8; i++)
        {
            Numbers[i] = (Alphabet.IndexOf(SeedVar[i * 2]) * 52) + Alphabet.IndexOf(SeedVar[(i * 2) + 1]);
            NumbersConcat += Numbers[i].ToString("0000");
        }

        Debug.LogFormat("[Abyss #{0}] The string of numbers obtained from the seed is {1}.", _moduleID, NumbersConcat);

        char[] NumbersConcat2 = NumbersConcat.ToCharArray();
        if (Bomb.GetBatteryCount() % 2 == 1)
            for (int i = 0; i < 16; i++)
            {
                char Cache = NumbersConcat2[i * 2];
                NumbersConcat2[i * 2] = NumbersConcat2[(i * 2) + 1];
                NumbersConcat2[(i * 2) + 1] = Cache;
            }
        else
            for (int i = 0; i < 16; i++)
            {
                char Cache = NumbersConcat2[(i * 2) + 1];
                NumbersConcat2[(i * 2) + 1] = NumbersConcat2[((i * 2) + 2) % 32];
                NumbersConcat2[((i * 2) + 2) % 32] = Cache;
            }
        string Cache2 = NumbersConcat2.Join("");

        Debug.LogFormat("[Abyss #{0}] After the first round of swaps ({1}), the string of numbers is {2}.", _moduleID, Bomb.GetBatteryCount() % 2 == 1 ? "odd" : "even", Cache2);

        if (int.Parse(Bomb.GetSerialNumber()[2].ToString()) % 2 == 1 && int.Parse(Bomb.GetSerialNumber()[5].ToString()) % 2 == 1)
        {
            NumbersConcat = Cache2.Substring(4, 4) + Cache2.Substring(0, 4)
                + Cache2.Substring(12, 4) + Cache2.Substring(8, 4)
                + Cache2.Substring(20, 4) + Cache2.Substring(16, 4)
                + Cache2.Substring(28, 4) + Cache2.Substring(24, 4);
        }
        else if (int.Parse(Bomb.GetSerialNumber()[2].ToString()) % 2 == 1)
        {
            NumbersConcat = Cache2.Substring(8, 4) + Cache2.Substring(12, 4)
                + Cache2.Substring(0, 4) + Cache2.Substring(4, 4)
                + Cache2.Substring(16, 4) + Cache2.Substring(20, 4)
                + Cache2.Substring(24, 4) + Cache2.Substring(28, 4);
        }
        else if (int.Parse(Bomb.GetSerialNumber()[2].ToString()) % 2 == 0 && int.Parse(Bomb.GetSerialNumber()[5].ToString()) % 2 == 1)
        {
            NumbersConcat = Cache2.Substring(16, 4) + Cache2.Substring(20, 4)
                + Cache2.Substring(24, 4) + Cache2.Substring(28, 4)
                + Cache2.Substring(0, 4) + Cache2.Substring(4, 4)
                + Cache2.Substring(8, 4) + Cache2.Substring(12, 4);
        }
        else
        {
            NumbersConcat = Cache2.Substring(28, 4) + Cache2.Substring(24, 4)
                + Cache2.Substring(20, 4) + Cache2.Substring(16, 4)
                + Cache2.Substring(12, 4) + Cache2.Substring(8, 4)
                + Cache2.Substring(4, 4) + Cache2.Substring(0, 4);
        }

        Debug.LogFormat("[Abyss #{0}] After the second round of swaps ({1}, {2}), the string of numbers is {3}.", _moduleID, int.Parse(Bomb.GetSerialNumber()[2].ToString()) % 2 == 1 ? "odd" : "even", int.Parse(Bomb.GetSerialNumber()[5].ToString()) % 2 == 1 ? "odd" : "even", NumbersConcat);

        int Ports = (4 - (Bomb.GetPortCount() % 4)) % 4;
        for (int i = 0; i < 8; i++)
        {
            NumbersConcat2[i * 4] = NumbersConcat[(i * 4) + Ports];
            NumbersConcat2[(i * 4) + 1] = NumbersConcat[(i * 4) + ((Ports + 1) % 4)];
            NumbersConcat2[(i * 4) + 2] = NumbersConcat[(i * 4) + ((Ports + 2) % 4)];
            NumbersConcat2[(i * 4) + 3] = NumbersConcat[(i * 4) + ((Ports + 3) % 4)];
        }

        Debug.LogFormat("[Abyss #{0}] After rotating the letters forwards by {1}, the string of numbers is {2}.", _moduleID, Bomb.GetPortCount() % 4, NumbersConcat2.Join(""));

        int[] ToEnter = new int[2];
        for (int i = 0; i < 6; i += 3)
            for (int j = 0; j < 3; j++)
                if (!LetterLess(Bomb.GetSerialNumber()[i + j]))
                    ToEnter[i / 3] += new int[] { 4, 2, 1 }[j];
        Debug.Log(ToEnter.Join());
        if (ToEnter[1] == ToEnter[0])
            ToEnter[1] = (ToEnter[1] + 1) % 8;

        NumbersConcat = NumbersConcat2.Join("").Substring(ToEnter[0] * 4, 4) + NumbersConcat2.Join("").Substring(ToEnter[1] * 4, 4);

        Debug.LogFormat("[Abyss #{0}] The string of numbers which needs to be entered is {1}.", _moduleID, NumbersConcat);
    }

    void CalcInput()
    {
        if (ForePresses == 0 && PrevPresses > 2 && OverOneSecond)
        {
            Audio.PlaySoundAtTransform("init", Orb.transform);
            Input = "";
            LastInput.text = "";
        }
        else if (ForePresses == 0 && PrevPresses > 1 && OverOneSecond)
        {
            Audio.PlaySoundAtTransform("backspace", Orb.transform);
            if (Input.Length == 1)
            {
                Input = "";
                LastInput.text = "";
            }
            else if (Input != "")
            {
                Input = Input.Substring(0, Input.Length - 1);
                LastInput.text = Input.Last().ToString();
            }
        }
        else if (ForePresses == 0 && PrevPresses == 1 && OverOneSecond)
        {
            LastInput.text = "";
            if (Input == NumbersConcat)
            {
                Debug.LogFormat("[Abyss #{0}] You entered {1}, which is correct. Module solved!", _moduleID, Input);
                StartCoroutine(Solve());
            }
            else
            {
                Debug.LogFormat("[Abyss #{0}] You entered {1}, which is incorrect. Strike!", _moduleID, Input == "" ? "nothing" : Input);
                Input = "";
                StartCoroutine(Strike());
            }
        }
        else if (OverOneSecond)
        {
            Audio.PlaySoundAtTransform("input", Orb.transform);
            Input += Mathf.Min(ForePresses, 9).ToString();
            LastInput.text = Input.Last().ToString();
        }
        if (OverOneSecond)
        {
            ForePresses = 0;
            PrevPresses = 0;
        }
    }

    void OrbPress()
    {
        Orb.AddInteractionPunch(0.25f);
        Audio.PlaySoundAtTransform("press", Orb.transform);
        if (OverOneSecond)
            ForePresses++;
        else
            PrevPresses++;
        if (Running)
            StopCoroutine(RunningCoroutine);
        RunningCoroutine = StartCoroutine(LightFlash());
    }

    private IEnumerator ReadyPress()
    {
        OrbHighlight.SetActive(false);
        ReadyButton.transform.localScale = new Vector3();
        float Timer = 0;
        Audio.PlaySoundAtTransform("init", Orb.transform);
        while (Timer < 0.5f)
        {
            Orb.transform.localScale = new Vector3(Easing.InOutExpo(Timer, 0, 0.1f, 0.5f), Easing.InOutExpo(Timer, 0, 0.1f, 0.5f), Easing.InOutExpo(Timer, 0, 0.1f, 0.5f));
            Seed.transform.localScale = new Vector3(Easing.InExpo(Timer, 1f, 0, 0.5f), Easing.InExpo(Timer, 1f, 0, 0.5f), Easing.InExpo(Timer, 1f, 0, 0.5f));
            Seed.transform.localEulerAngles = new Vector3(90, 0, Easing.InExpo(Timer, 0, -45f, 0.5f));
            yield return null;
            Timer += Time.deltaTime;
        }
        Orb.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        Seed.transform.localScale = new Vector3(0, 0, 0);
        OrbHighlight.SetActive(true);
    }

    private IEnumerator OrbAnim()
    {
        while (true)
        {
            Orb.GetComponent<MeshRenderer>().material.mainTextureOffset += new Vector2(0, 0.01f);
            yield return null;
        }
    }

    private IEnumerator OrbHighlighted()
    {
        float Timer = 0;
        while (Timer < 1f && Highlighting)
        {
            yield return null;
            Timer += Time.deltaTime;
        }
        if (Highlighting)
        {
            OverOneSecond = true;
            StartCoroutine(OpenLight());
            Sound = Audio.PlaySoundAtTransformWithRef("ambience", Orb.transform);
            PlayingSound = true;
            if (!DisableFilter)
                StartCoroutine(FadeIn());
        }
    }

    private IEnumerator OpenLight()
    {
        float Timer = 0;
        while (Timer < 0.5f)
        {
            Light.color = new Color(Light.color.r, Light.color.g, Light.color.b, Mathf.Lerp(0, 1f, Timer * 2));
            yield return null;
            Timer += Time.deltaTime;
        }
    }

    private IEnumerator CloseLight()
    {
        if (!DisableFilter)
            StartCoroutine(FadeOut());
        Sound.StopSound();
        float Timer = 0;
        while (Timer < 0.5f)
        {
            Light.color = new Color(Light.color.r, Light.color.g, Light.color.b, Mathf.Lerp(1f, 0, Timer * 2));
            yield return null;
            Timer += Time.deltaTime;
        }
    }

    private IEnumerator LightFlash()
    {
        Running = true;
        float Timer = 0;
        float TimeLimit = 0.5f;
        while (Timer < TimeLimit)
        {
            Light.color = new Color(Mathf.Lerp(0, 1f, Timer * (1 / TimeLimit)), Mathf.Lerp(0, 1f, Timer * (1 / TimeLimit)), Mathf.Lerp(0, 1f, Timer * (1 / TimeLimit)), Light.color.a);
            yield return null;
            Timer += Time.deltaTime;
        }
        Running = false;
    }

    private IEnumerator FadeIn(float Speed = 2f)
    {
        if (PostProcess != null)
        {
            DestroyImmediate(PostProcess);
        }
        try
        {
            PostProcess = MainCameraTransform.gameObject.AddComponent<CameraPostProcess>();
            PostProcess.PostProcessMaterial = new Material(PostProcessMaterial);
        }
        catch { }
        for (float Progress = 0.0f; Progress < 1.0f; Progress += Time.deltaTime * Speed)
        {
            try
            {
                PostProcess.Vignette = Progress * 1.6f;
                PostProcess.Grayscale = Progress;
            }
            catch { }
            yield return null;
        }
        try
        {
            PostProcess.Vignette = 1.6f;
            PostProcess.Grayscale = 1f;
        }
        catch { }
    }

    private IEnumerator FadeOut(float speed = 4f)
    {
        for (float Progress = 1.0f - Time.deltaTime * speed; Progress >= 0.0f; Progress -= Time.deltaTime * speed)
        {
            try
            {
                PostProcess.Vignette = Progress * 1.6f;
                PostProcess.Grayscale = Progress;
            }
            catch { }
            yield return null;
        }

        if (PostProcess != null)
        {
            DestroyImmediate(PostProcess);
            PostProcess = null;
        }
    }

    private IEnumerator Solve()
    {
        Orb.AddInteractionPunch(3f);
        Audio.PlaySoundAtTransform("solve", Orb.transform);
        Module.HandlePass();
        Solved = true;
        Seed.text = "SOLVED";
        Seed.color = new Color32(0, 255, 0, 64);
        Seed.characterSize += 0.0002f;
        float Timer = 0;
        while (Timer < 0.5f)
        {
            Orb.transform.localScale = new Vector3(Easing.InOutExpo(Timer, 0.1f, 0, 0.5f), Easing.InOutExpo(Timer, 0.1f, 0, 0.5f), Easing.InOutExpo(Timer, 0.1f, 0, 0.5f));
            Seed.transform.localScale = new Vector3(Easing.OutExpo(Timer, 0, 1f, 0.5f), Easing.OutExpo(Timer, 0, 1f, 0.5f), Easing.OutExpo(Timer, 0, 1f, 0.5f));
            Seed.transform.localEulerAngles = new Vector3(90, 0, Easing.OutExpo(Timer, 45f, 0, 0.5f));
            yield return null;
            Timer += Time.deltaTime;
        }
        Orb.transform.localScale = new Vector3(0, 0, 0);
        Seed.transform.localScale = new Vector3(1, 1, 1);
        Seed.transform.localEulerAngles = new Vector3(90, 0, 0);
    }

    private IEnumerator Strike()
    {
        Module.HandleStrike();
        OrbHighlight.SetActive(false);
        float Timer = 0;
        Audio.PlaySoundAtTransform("strike", Orb.transform);
        while (Timer < 0.5f)
        {
            Orb.transform.localScale = new Vector3(Easing.InOutExpo(Timer, 0.1f, 0, 0.5f), Easing.InOutExpo(Timer, 0.1f, 0, 0.5f), Easing.InOutExpo(Timer, 0.1f, 0, 0.5f));
            Seed.transform.localScale = new Vector3(Easing.OutExpo(Timer, 0, 1f, 0.5f), Easing.OutExpo(Timer, 0, 1f, 0.5f), Easing.OutExpo(Timer, 0, 1f, 0.5f));
            Seed.transform.localEulerAngles = new Vector3(90, 0, Easing.OutExpo(Timer, 45f, 0, 0.5f));
            yield return null;
            Timer += Time.deltaTime;
        }
        Orb.transform.localScale = new Vector3(0, 0, 0);
        Seed.transform.localScale = new Vector3(1, 1, 1);
        Seed.transform.localEulerAngles = new Vector3(90, 0, 0);
        OrbHighlight.SetActive(true);
        ReadyButton.transform.localScale = new Vector3(1, 1, 1);
        PressedReady = false;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} rdc12345s' to press the ready button, delete your previous input, clear your input, enter 12345, then submit.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        DisableFilter = true;
        command = command.ToLowerInvariant();
        if (command == null)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        for (int i = 0; i < command.Length; i++)
        {
            if (!"rsdc0123456789".Contains(command[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] != 'r' && !PressedReady)
            {
                yield return "sendtochaterror The ready button has not been pressed yet!";
                yield break;
            }
            switch (command[i])
            {
                case 'r':
                    if (!PressedReady)
                        ReadyButton.OnInteract();
                    else
                    {
                        yield return "sendtochaterror The ready button has already been pressed!";
                        yield break;
                    }
                    while (!OrbHighlight.activeInHierarchy)
                        yield return new WaitForSeconds(0.1f);
                    break;
                case 's':
                    Orb.OnInteract();
                    Orb.OnHighlight();
                    while (!OverOneSecond)
                        yield return new WaitForSeconds(0.1f);
                    Orb.OnHighlightEnded();
                    break;
                case 'd':
                    Orb.OnInteract();
                    yield return new WaitForSeconds(0.1f);
                    Orb.OnInteract();
                    Orb.OnHighlight();
                    while (!OverOneSecond)
                        yield return new WaitForSeconds(0.1f);
                    Orb.OnHighlightEnded();
                    break;
                case 'c':
                    Orb.OnInteract();
                    for (int j = 0; j < 2; j++)
                    {
                        yield return new WaitForSeconds(0.1f);
                        Orb.OnInteract();
                    }
                    Orb.OnHighlight();
                    while (!OverOneSecond)
                        yield return new WaitForSeconds(0.1f);
                    Orb.OnHighlightEnded();
                    break;
                default:
                    Orb.OnHighlight();
                    while (!OverOneSecond)
                        yield return new WaitForSeconds(0.1f);
                    for (int j = 0; j < int.Parse(command[i].ToString()); j++)
                    {
                        Orb.OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                    Orb.OnHighlightEnded();
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        DisableFilter = true;
        if (!PressedReady)
        {
            ReadyButton.OnInteract();
            while (!OrbHighlight.activeInHierarchy)
                yield return true;
        }
        if (Input != NumbersConcat.Substring(0, Input.Length))
        {
            Orb.OnInteract();
            for (int j = 0; j < 2; j++)
            {
                yield return true;
                Orb.OnInteract();
            }
            Orb.OnHighlight();
            while (!OverOneSecond)
                yield return true;
            Orb.OnHighlightEnded();
            yield return true;
        }
        for (int i = Input.Length; i < 8; i++)
        {
            Orb.OnHighlight();
            while (!OverOneSecond)
                yield return true;
            for (int j = 0; j < int.Parse(NumbersConcat[i].ToString()); j++)
            {
                Orb.OnInteract();
                yield return true;
            }
            Orb.OnHighlightEnded();
            yield return true;
        }
        Orb.OnInteract();
        Orb.OnHighlight();
        while (!OverOneSecond)
            yield return true;
        Orb.OnHighlightEnded();
    }
}
