using UnityEngine;
using Unity.InferenceEngine; 
using Unity.InferenceEngine.Tokenization;
using Unity.InferenceEngine.Tokenization.Parsers.HuggingFace;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class TheCrossroads : MonoBehaviour
{
    public string modelname = "model.onnx"; 
    public ModelAsset modelasset; 
    public string tokenizername = "tokenizer.json";

    private Model model;
    private ITokenizer tokenizer; 

    void Start()
    {
        var parser = HuggingFaceParser.GetDefault();
        string pathfulltokenizer = Path.Combine(Application.streamingAssetsPath, tokenizername);
        tokenizer = parser.Parse(File.ReadAllText(pathfulltokenizer));
        
        string pathfullmodel = Path.Combine(Application.streamingAssetsPath, modelname);
        model = ModelLoader.Load(pathfullmodel); 
    }


    public string BestOption(string[] optionsnames, string[] options, string obj, string desc, string action)
    {
        Debug.Log("I have been summoned");
        List<float> scores = new List<float>();

        string prompt = $"Pay extreme attention to actions and negative words. \nThere is a {obj}, {desc}.\n You{action}\n";

        List<string> answersfull = new List<string>();
        for (int i = 0; i < optionsnames.Length; ++i) answersfull.Add(prompt + options[i]);

        int batchsize = optionsnames.Length;
        int maxlen = 0;
        
        List<int[]> rawids = new List<int[]>();
        List<int[]> rawmasks = new List<int[]>();

        for (int i = 0; i < optionsnames.Length; ++i)
        {
            var encoding = tokenizer.Encode(answersfull[i]);
            int[] ids = encoding.GetIds().ToArray();
            int[] mask = encoding.GetAttentionMask().ToArray();

            rawids.Add(ids);
            rawmasks.Add(mask);

            if (ids.Length > maxlen) maxlen = ids.Length;
        }

        int tokenstotal = batchsize * maxlen;
        int[] flatids = new int[tokenstotal];
        int[] flatmasks = new int[tokenstotal];

        int padtoken = 151643;

        for (int i = 0; i < batchsize; ++i)
        {
            for (int m = 0; m < maxlen; ++m)
            {
                if (m < rawids[i].Length)
                {
                    flatids[i * maxlen + m] = rawids[i][m];
                    flatmasks[i * maxlen + m] = rawmasks[i][m];
                }
                else
                {
                    flatids[i * maxlen + m] = padtoken;
                    flatmasks[i * maxlen + m] = 0;
                }
            }
        }

        TensorShape batchshape = new TensorShape(batchsize, maxlen);
        
        Tensor<int> inputids = new Tensor<int>(batchshape, flatids);
        Tensor<int> attentionmask = new Tensor<int>(batchshape, flatmasks);

        int[] positionidsflat = new int[batchsize * maxlen];
        for (int i = 0; i < batchsize; ++i)
        {
            for (int m = 0; m < maxlen; ++m) positionidsflat[(i * maxlen) + m] = m;
        }

        Tensor<int> positionids = new Tensor<int>(batchshape, positionidsflat);

        Worker worker = new Worker(model, BackendType.GPUCompute);
        worker.SetInput("input_ids", inputids);
        worker.SetInput("attention_mask", attentionmask);
        worker.SetInput("position_ids", positionids);
        worker.Schedule();
        Tensor<float> outputlogits = worker.PeekOutput("logits") as Tensor<float>;
        int vocablogits = worker.PeekOutput("logits").shape[2];


        float[] logitsreal = outputlogits.DownloadToArray();

        inputids.Dispose();
        attentionmask.Dispose();
        worker.Dispose();

        for (int i = 0; i < batchsize; ++i)
        {
            float thisscore = 0;
            for (int m = 0; m < maxlen; ++m) 
            {
                if (flatmasks[i * maxlen + m] == 0) continue;

                int thistokenid = flatids[i * maxlen + m];
                int logitstartshere = (i * maxlen + m) * vocablogits;
                
                float maxlogit = float.MinValue;
                for (int o = 0; o < vocablogits; o++)
                {
                    float thislogit = logitsreal[logitstartshere + o];
                    if (thislogit > maxlogit) maxlogit = thislogit;
                }

                double total = 0.0;
                for (int o = 0; o < vocablogits; o++)
                {
                    total += Mathf.Exp(logitsreal[logitstartshere + o] - maxlogit);
                }
                double logtotal = Mathf.Log((float)total);

                float targetLogit = logitsreal[logitstartshere + thistokenid];
                float logprob = (float)((targetLogit - maxlogit) - logtotal);

                thisscore += logprob;
            }
            scores.Add(thisscore);
        }

        int minindex = 0; float minval = scores[0];
        for (int i = 1; i < batchsize; ++i) 
        {
            Debug.Log(scores[i]);
            if (scores[i] < minval)
            {
                minindex = i;
                minval = scores[i];
            }
        }

        Debug.Log(optionsnames[minindex]);
        return optionsnames[minindex];

    }
}