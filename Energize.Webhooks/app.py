import json, requests
from flask import Flask, request, abort

def deserialize(data):
    return json.loads(data)

def load_config():
    f = open("config.json", 'r')
    config = deserialize(f.read())
    f.close()
    return config

app = Flask(__name__)
config = load_config()
endpoint = "https://discordapp.com/api/webhooks/" + config["webhook_id"] + "/" + config["webhook_token"]
dblpage = "https://discordbots.org/bot/" + config["bot"]
authorization = "Bot " + config["token"]
avatar_url = "https://cdn.discordapp.com/icons/264445053596991498/ec576186a89084914d3c70e8d9f4d2c3.jpg"

@app.before_request
def only_json():
    if not request.is_json:
        abort(400)
    if request.headers["authorization"] != config["authorization"]:
        abort(403)

def http_post(url, data):
    headers = {
        "User-Agent": "Energize (Discord Bot)",
        "Content-Type": "application/json",
        "Authorization": authorization
    }
    data = json.dumps(data)
    requests.post(url, headers=headers, data=data)

def exec_webhook(data):
    http_post(endpoint, data)
    print(data)

def dbl_upvote(webhook):
    multiplier = ""
    if(webhook["isWeekend"]):
        multiplier = " (x2)"
    exec_webhook({
        "username": "Discord Bot List",
        "avatar_url": avatar_url,
        "content": "<" + dblpage + ">",
        "tts": False,
        "file": "",
        "embeds": [
            {
                "description": "Upvote on Energize" + multiplier,
                "type": "rich",
                "color":  0x7289da
            }
        ],
        "payload_json": ""
    })

@app.route('/', methods=["GET","POST"])
def main():
    webhook = deserialize(request.data)
    render = ""
    if webhook["bot"] == config["bot"]:
        dbl_upvote(webhook)
        render = "sent"
    return render