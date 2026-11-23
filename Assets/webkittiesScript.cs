using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class webkittiesScript : MonoBehaviour
{

	public List<Texture> cats;
	public KMSelectable upButton, downButton, submitButton;
	public TextMesh codeText, descText;
	public MeshRenderer catScreenRenderer;
	public Texture solveTexture;
	public KMAudio audio;
	public AudioClip solveSound;
	public KMBombModule module;
	
	private readonly Dictionary<string, string> codeDescDict = new Dictionary<string, string>
	{
		{"100", "Continue"},
		{"101", "Switching Protocols"},
		{"102", "Processing"},
		{"103", "Early Hints"},
		{"200", "OK"},
		{"201", "Created"},
		{"202", "Accepted"},
		{"203", "Non-Authoritative\nInformation"},
		{"204", "No Content"},
		{"205", "Reset Content"},
		{"206", "Partial Content"},
		{"207", "Multi-Status"},
		{"208", "Already Reported"},
		{"214", "Transformation\nApplied"},
		{"226", "IM Used"},
		{"300", "Multiple Choices"},
		{"301", "Moved Permanently"},
		{"302", "Found"},
		{"303", "See Other"},
		{"304", "Not Modified"},
		{"305", "Use Proxy"},
		{"307", "Temporary Redirect"},
		{"308", "Permanent Redirect"},
		{"400", "Bad Request"},
		{"401", "Unauthorized"},
		{"402", "Payment Required"},
		{"403", "Forbidden"},
		{"404", "Not Found"},
		{"405", "Method Not Allowed"},
		{"406", "Not Acceptable"},
		{"407", "Proxy Authentication\nRequired"},
		{"408", "Request Timeout"},
		{"409", "Conflict"},
		{"410", "Gone"},
		{"411", "Length Required"},
		{"412", "Precondition Failed"},
		{"413", "Payload Too Large"},
		{"414", "Request-URI\nToo Long"},
		{"415", "Unsupported\nMedia Type"},
		{"416", "Request Range\nNot Satisfiable"},
		{"417", "Expectation Failed"},
		{"418", "I’m a teapot"},
		{"419", "Page Expired"},
		{"420", "Enhance Your Calm"},
		{"421", "Misdirected\nRequest"},
		{"422", "Unprocessable\nEntity"},
		{"423", "Locked"},
		{"424", "Failed Dependency"},
		{"425", "Too Early"},
		{"426", "Upgrade Required"},
		{"428", "Precondition\nRequired"},
		{"429", "Too Many Requests"},
		{"431", "Request Header\nFields Too Large"},
		{"444", "No Response"},
		{"451", "Unavailable For\nLegal Reasons"},
		{"495", "SSL Certificate\nError"},
		{"496", "SSL Certificate\nRequired"},
		{"497", "HTTP Request Sent\nto HTTPS Port"},
		{"498", "Token expired/invalid"},
		{"499", "Client Closed\nRequest"},
		{"500", "Internal Server\nError"},
		{"501", "Not Implemented"},
		{"502", "Bad Gateway"},
		{"503", "Service Unavailable"},
		{"504", "Gateway Timeout"},
		{"506", "Variant Also\nNegotiates"},
		{"507", "Insufficient Storage"},
		{"508", "Loop Detected"},
		{"509", "Bandwidth Limit\nExceeded"},
		{"510", "Not Extended"},
		{"511", "Network\nAuthentication Required"},
		{"521", "Web Server Is Down"},
		{"522", "Connection Timed Out"},
		{"523", "Origin Is\nUnreachable"},
		{"525", "SSL Handshake Failed"},
		{"530", "Site Frozen"},
		{"599", "Network Connect\nTimeout Error"}
	};

	private string correctAnswer = "100";
	private int generatedStages;
	private bool ModuleSolved;

	private static int ModuleIdCounter;
	private int ModuleId;
	void log(string msg){Debug.Log("[Webkitties #"+ModuleId+"] "+msg);}
	void Awake(){ModuleId=++ModuleIdCounter;}

	void setCodeDesc(string code)
	{
		codeText.text = code;
		descText.text = codeDescDict[code];
	}

	void generateStage()
	{
		setCodeDesc(correctAnswer);
		correctAnswer = codeDescDict.Keys.ToList()[Random.Range(0, codeDescDict.Keys.Count)];
		log("Next code: "+correctAnswer);
		catScreenRenderer.material.mainTexture = cats.First(x=>x.name==correctAnswer);
		generatedStages++;
	}

	IEnumerator solve()
	{
		ModuleSolved = true;
		audio.PlaySoundAtTransform(solveSound.name, transform);
		float timer = 0f, timerOffset = 0f;
		while (timer < 1.53f)
		{
			if (timer >= timerOffset)
			{
				string randomCode = codeDescDict.Keys.ToList().PickRandom();
				setCodeDesc(randomCode);
				catScreenRenderer.material.mainTexture = cats.First(x => x.name == randomCode);
				timerOffset += 0.05f;
			}
			timer += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		module.HandlePass();
		log("Module solved!");
		codeText.text = ":3";
		catScreenRenderer.material.mainTexture = solveTexture;
		//descText.text = "Solved!";
		for (int i = 1; i <= 7; i++)
		{
			descText.text = "Solved!".Substring(0, i);
			yield return new WaitForSeconds(0.03f);
		}
	}
	
	// Use this for initialization
	void Start ()
	{
		generateStage();
		upButton.OnInteract = delegate()
		{
			audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (ModuleSolved) return false;
			setCodeDesc(codeDescDict.Keys.ToList()[(codeDescDict.Keys.ToList().IndexOf(codeText.text)+codeDescDict.Count-1)%codeDescDict.Count]);
			return false;
		};
		downButton.OnInteract = delegate()
		{
			audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (ModuleSolved) return false;
			setCodeDesc(codeDescDict.Keys.ToList()[(codeDescDict.Keys.ToList().IndexOf(codeText.text)+1)%codeDescDict.Count]);
			return false;
		};
		submitButton.OnInteract = delegate()
		{
			audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (ModuleSolved) return false;
			if (codeText.text == correctAnswer)
			{
				log("Submitted "+codeText.text+", which is correct.");
				if (generatedStages == 3)
					StartCoroutine(solve());
				else generateStage();
			}
			else
			{
				log("Submitted "+codeText.text+", which is wrong. Strike.");
				module.HandleStrike();
			}
			return true;
		};
	}

	private string TwitchHelpMessage = "Use !{0} <code> to input the code.";

	IEnumerator ProcessTwitchCommand(string command){
		if (!codeDescDict.Keys.ToList().Contains(command)){
			yield return "sendtochaterror Invalid code.";
			yield break;
		}
		yield return null;
		int i = codeDescDict.Count()+1;
		while(i>0){
			i--;
			if (command != codeText.text){
				downButton.OnInteract();
				yield return new WaitForSeconds(.05f);
			} else break;
		}
		if (command != codeText.text){
			yield return "sendtochaterror Something went wrong.";
			yield break;
		}
		yield return new WaitForSeconds(.05f);
		yield return "solve";
		yield return "strike";
		submitButton.OnInteract();
	}

	IEnumerator TwitchForcedSolve(){
		yield return null;
		yield return solve();
	}
}
