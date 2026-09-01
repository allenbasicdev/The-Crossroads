!pip install uv
!uv pip install guidance
!uv pip install transformers
!uv pip install huggingface_hub
!uv pip install outlines
!uv pip install pydantic

from huggingface_hub import snapshot_download
import torch
import outlines
from outlines import models
from transformers import AutoModelForCausalLM, AutoTokenizer
from guidance import models, gen, json

snapshot_download(repo_id = "Qwen/Qwen2.5-0.5B-Instruct", local_dir = "model")

model = AutoModelForCausalLM.from_pretrained("model", device_map = "cuda", trust_remote_code = True)
tokenize = AutoTokenizer.from_pretrained("model", trust_remote_code = True)

modelreal = models.Transformers(model, tokenize)

from typing import Literal
from pydantic import BaseModel, Field, create_model
from enum import Enum
from guidance import select
import numpy as np

def findbest(optionsnames, options, obj, desc, action):
    prompt = "Pay extreme attention to actions and negative words. \nThere is a " + obj + ", " + desc + ".\n You" + action + "\n"

    prompttokens = (tokenize(prompt, return_tensors="pt").to("cuda"))["input_ids"].shape[1]

    tokenize.padding_side = "right"
    if tokenize.pad_token is None:
        tokenize.pad_token = tokenize.eos_token
      
    answersfull = []
    for i in range(0, len(options)):
        answersfull.append(prompt + options[i])
      
    inputs = tokenize(answersfull, return_tensors="pt", padding = True).to("cuda")
    inputids = inputs["input_ids"]
    attentionmask = inputs["attention_mask"]

    with torch.no_grad():
        outputs = model(**inputs)
        outputlogits = outputs.logits

    logitshift = outputlogits[..., :-1, :].contiguous()
    labelshift = inputids[..., 1:].contiguous()
    attentionmaskshift = attentionmask[..., 1:].contiguous()

    logprobs = torch.nn.functional.log_softmax(logitshift, dim=-1)
    alllogprobs = torch.gather(logprobs, dim=-1, index = labelshift.unsqueeze(-1)).squeeze(-1) * attentionmaskshift

    scores = []
    for i in range(0, len(options)):
        thislogprobs = alllogprobs[i]
        logprobsreal = thislogprobs[attentionmaskshift[i].bool()][(prompttokens - 1):]
      
        scores.append(logprobsreal.mean().item())
        print(logprobsreal.mean().item())

    maxindex = 0; maxval = scores[0]
    for i in range(0, len(options)):
        if scores[i] > maxval:
            maxindex = i
            maxval = scores[i]

    print(optionsnames[maxindex])
