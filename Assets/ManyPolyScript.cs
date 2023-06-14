using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManyPolyScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMSelectable modselect;
    public List<KMSelectable> buttons;
    public Renderer[] brends;
    public Material[] sectors;
    public TextMesh[] tempos;
    public Transform[] speen;
    public Transform speaker;
    public AudioClip[] sounds;
    public GameObject matstore;

    private readonly Color[] cols = new Color[] { new Color(0.185f, 0.605f, 0.125f), new Color(1, 0.43f, 0.345f), new Color(0.88f, 0.79f, 0.54f), new Color(1, 0.92f, 0.32f), new Color(0.73f, 0.64f, 0.71f), new Color(0.84f, 0.64f, 0.28f), new Color(0.67f, 0.37f, 0.85f), new Color(0.25f, 0.6f, 0.73f), new Color(1, 0.5f, 0.9f), new Color(0.66f, 0.87f, 0.45f), new Color(0.275f, 0.36f, 0.705f), new Color(0.47f, 0.775f, 0.61f), new Color(0.75f, 0.1f, 0.41f), new Color(0.69f, 0.25f, 0.75f), new Color(0.45f, 0.45f, 0.45f)};
    private int[] freq = new int[5];
    private int[] sound;
    private int fselect;
    private float dur;
    private bool[] sub = new bool[5];
    private float beat = 0.75f;
    private bool focus;
    private bool up;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private int HCF(int a, int b)
    {
        int c = 1;
        for (int i = 2; i <= Mathf.Min(a, b); i++)
            if (a % i == 0 && b % i == 0)
                c = i;
        return c;
    }

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        fselect = 1;
        freq[0] = Random.Range(2, 11);
        List<int> availablefrequencies = Enumerable.Range(2, 9).ToList();
        for (int i = 1; i < 3; i++)
        {
            for(int j = 0; j < availablefrequencies.Count(); j++)
                if(HCF(freq[i - 1], availablefrequencies[j]) > 1)
                {
                    availablefrequencies.RemoveAt(j);
                    j--;
                }
            freq[i] = availablefrequencies.PickRandom();
        }
        availablefrequencies.Clear();
        for(int i = 4; i < 21; i++)
        {
            if (freq.Contains(i))
                continue;
            bool[] mul = new bool[3];
            for (int j = 0; j < 3; j++)
                mul[j] = HCF(freq[j], i) > 1;
            if (mul.Count(x => x) == 1)
                availablefrequencies.Add(i);
        }
        freq[3] = availablefrequencies.PickRandom();
        availablefrequencies.Clear();
        for(int i = 30; i < 61; i++)
        {
            bool[] mul = new bool[3];
            for (int j = 0; j < 3; j++)
                mul[j] = HCF(freq[j], i) > 1;
            if (mul.Count(x => x) > 1)
                availablefrequencies.Add(i);
        }
        freq[4] = availablefrequencies.PickRandom();
        dur = freq[4] * Random.Range(0.35f, 0.75f);
        freq = freq.Shuffle();
        sound = Enumerable.Range(0, 15).ToArray().Shuffle().Take(5).ToArray();
        Debug.LogFormat("[Many Poly #{0}] The sounds played by module are: {1}.", moduleID, string.Join(", ", sound.Select(x => sounds[x].name).ToArray()));
        Debug.LogFormat("[Many Poly #{0}] The frequencies of each sound (over a {1} second loop) are: {2}", moduleID, dur.ToString("f2"), string.Join(", ", freq.Select(x => x.ToString()).ToArray()));
        for (int i = 0; i < 5; i++)
        {
            brends[i].material.color = cols[sound[i]];
            brends[i + 5].material = sectors[freq[i]];
            brends[i + 5].material.color = new Color(0, 0, 0);
            tempos[i].color = cols[sound[i]];
            StartCoroutine(Play(i, dur / freq[i]));
        }
        StartCoroutine(Beat());   
        modselect.OnFocus += delegate () { focus = true; };
        modselect.OnDefocus += delegate () { focus = false; };
        foreach(KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (!moduleSolved)
                {
                    switch (b)
                    {
                        case 0:
                        case 1:
                            button.AddInteractionPunch(0.3f);
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);
                            up = b == 0;
                            StartCoroutine("F");
                            break;
                        default:
                            int k = b - 2;
                            if (!sub[k])
                            {
                                button.AddInteractionPunch();
                                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                                if (freq[k] == fselect)
                                {
                                    sub[k] = true;
                                    brends[k + 5].enabled = true;
                                    tempos[k].text = fselect.ToString();
                                    if(fselect > 9)
                                        tempos[k].transform.localScale = new Vector3(0.0009f, 0.0013f, 0.001f);
                                    if(sub.All(x => x))
                                    {
                                        tempos[5].text = "";
                                        moduleSolved = true;
                                        module.HandlePass();
                                        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                                        for(int i = 0; i < 5; i++)
                                        {
                                            brends[i].material.color = new Color(0, 0, 0);
                                            brends[i + 5].material.color = new Color(1, 1, 1);
                                            tempos[i].color = new Color(1, 1, 1);
                                        }
                                    }
                                }
                                else
                                    module.HandleStrike();
                            }
                            break;
                    }
                }
                return false;
            };
        }
        buttons[0].OnInteractEnded += delegate ()
        {
            StopCoroutine("F");
        };
        buttons[1].OnInteractEnded += delegate ()
        {
            StopCoroutine("F");
        };
        matstore.SetActive(false);
    }

    private IEnumerator Play(int i, float d)
    {
        while (module.gameObject.activeSelf)
        {
            speen[i].transform.localEulerAngles += new Vector3(0, 360 / freq[i], 0);
            if (!moduleSolved && (focus || Application.isEditor))
            {
                beat += 0.05f;
                Audio.PlaySoundAtTransform(sounds[sound[i]].name, speaker);
            }
            yield return new WaitForSeconds(d);
        }
    }

    private IEnumerator F()
    {
        Audio.PlaySoundAtTransform("tick", transform);
        if (up ? fselect > 1 : fselect < 60)
        Count();
        yield return new WaitForSeconds(0.3f);
        int f = fselect % 2;
        while (1 < fselect && fselect < 60)
        {
            Count();
            if(fselect % 2 == f)
                Audio.PlaySoundAtTransform("tick", transform);
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void Count()
    {
        fselect += up ? -1 : 1;
        tempos[5].text = (fselect < 10 ? "0" : "") + fselect;
    }

    private IEnumerator Beat()
    {
        while (!moduleSolved)
        {
            if(beat > 0.75f)
            {
                beat -= Time.deltaTime / 3;
                speaker.transform.localScale = new Vector3(beat, beat, 0.5f);
            }
            yield return null;
        }
        speaker.transform.localScale = new Vector3(0.75f, 0.75f, 0.5f);
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} listen <#> [Selects and focuses on the module for '#' seconds] | !{0} set <1-60> [Sets the display to the specified frequency] | !{0} button <1-5> [Selects the specified coloured button from left to right] | !{0} submit <1-60> <1-60> <1-60> <1-60> <1-60> [Submits all five frequencies]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (parameters[0].ToLower().Equals("listen"))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a number of seconds!";
            else
            {
                float temp;
                if (!float.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified number of seconds '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                yield return null;
                yield return "trywaitcancel " + parameters[1];
            }
            yield break;
        }
        if (parameters[0].ToLower().Equals("set"))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a frequency!";
            else
            {
                int temp;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified frequency '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 1 || temp > 60)
                {
                    yield return "sendtochaterror The specified frequency '" + parameters[1] + "' is out of range 1-60!";
                    yield break;
                }
                yield return null;
                if (temp > fselect)
                {
                    buttons[1].OnInteract();
                    while (temp != fselect)
                        yield return null;
                    buttons[1].OnInteractEnded();
                }
                else if (temp < fselect)
                {
                    buttons[0].OnInteract();
                    while (temp != fselect)
                        yield return null;
                    buttons[0].OnInteractEnded();
                }
            }
            yield break;
        }
        if (parameters[0].ToLower().Equals("button"))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a button!";
            else
            {
                int temp;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror!f The specified button '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 1 || temp > 5)
                {
                    yield return "sendtochaterror The specified button '" + parameters[1] + "' is out of range 1-5!";
                    yield break;
                }
                if (sub[temp - 1])
                {
                    yield return "sendtochaterror The specified button '" + parameters[1] + "' already has the correct frequency!";
                    yield break;
                }
                yield return null;
                buttons[temp + 1].OnInteract();
            }
            yield break;
        }
        if (parameters[0].ToLower().Equals("submit"))
        {
            if (parameters.Length > 6)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length >= 1 && parameters.Length <= 5)
                yield return "sendtochaterror Please specify all five frequencies!";
            else
            {
                for (int i = 1; i < 6; i++)
                {
                    int temp;
                    if (!int.TryParse(parameters[i], out temp))
                    {
                        yield return "sendtochaterror!f The specified frequency '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    if (temp < 1 || temp > 60)
                    {
                        yield return "sendtochaterror The specified frequency '" + parameters[i] + "' is out of range 1-60!";
                        yield break;
                    }
                    if (sub[i - 1] && freq[i - 1] != temp)
                    {
                        yield return "sendtochaterror The specified frequency '" + parameters[i] + "' does not match the frequency already submitted for button '" + i + "'!";
                        yield break;
                    }
                }
                yield return null;
                while (!sub.All(x => x))
                {
                    int smallest = 99;
                    int smallestIndex = -1;
                    for (int j = 0; j < 5; j++)
                    {
                        if (!sub[j] && System.Math.Abs(int.Parse(parameters[j + 1]) - fselect) < smallest)
                        {
                            smallest = System.Math.Abs(int.Parse(parameters[j + 1]) - fselect);
                            smallestIndex = j;
                        }
                    }
                    if (int.Parse(parameters[smallestIndex + 1]) > fselect)
                    {
                        buttons[1].OnInteract();
                        while (int.Parse(parameters[smallestIndex + 1]) != fselect)
                            yield return null;
                        buttons[1].OnInteractEnded();
                        yield return new WaitForSeconds(.1f);
                    }
                    else if (int.Parse(parameters[smallestIndex + 1]) < fselect)
                    {
                        buttons[0].OnInteract();
                        while (int.Parse(parameters[smallestIndex + 1]) != fselect)
                            yield return null;
                        buttons[0].OnInteractEnded();
                        yield return new WaitForSeconds(.1f);
                    }
                    buttons[smallestIndex + 2].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return ProcessTwitchCommand("submit " + freq[0] + " " + freq[1] + " " + freq[2] + " " + freq[3] + " " + freq[4]);
    }
}
