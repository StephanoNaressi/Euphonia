// https://stackoverflow.com/a/21125098
function getCookie(name) {
    var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    if (match) return match[2];
}

async function getApiKeyAsync() {
    const resp = await fetch(window.config_remoteUrl + "php/getLastfmApiKey.php");
    return await resp.text();
}

async function makeAuthCallAsync(method, params)
{
    const apiKey = await getApiKeyAsync();
    if (apiKey === "")
    {
        return;
    }

    const sk = getCookie("lastfm_key");
    let tokenUrl = window.config_remoteUrl + `php/getAuthUrl.php?sk=${sk}&method=${method}`;
    for (const [key, value] of Object.entries(params)) {
        tokenUrl += `&${key}=${value}`;
    }
    const signResp = await fetch(tokenUrl);
    const signature = await signResp.text();

    const data = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
        data.append(key, value);
    }
    data.append("method", method);
    data.append("api_key", apiKey);
    data.append("sk", sk);
    data.append("api_sig", signature);

    const url = `https://ws.audioscrobbler.com/2.0/?format=json`;

    const resp = await fetch(url, {
        method: "POST",
        body: data
    });

    return await resp.json();
}

window.lastfm_registerScrobbleAsync = lastfm_registerScrobbleAsync;
async function lastfm_registerScrobbleAsync(song, artist, album, length)
{
    const json = await makeAuthCallAsync("track.updateNowPlaying", {
        artist : artist,
        track: song,
        album: album,
        duration: Math.ceil(length)
    });
    console.log(json);
}

window.lastfm_initAsync = lastfm_initAsync;
async function lastfm_initAsync()
{
    document.getElementById("lastfmLogin").addEventListener("click", async () => {
        const apiKey = await getApiKeyAsync();
        if (apiKey === "")
        {
            console.error("last.fm API key is not set");
        }
        else
        {
            window.location.href = `https://www.last.fm/api/auth/?api_key=${apiKey}&cb=${window.location.origin}`;
        }
    });

    const url = new URL(window.location.href);
    let token = url.searchParams.get("token");

    if (token !== undefined && token !== null) // We were redirected here from last.fm
    {
        const apiKey = await getApiKeyAsync();
        if (apiKey === "")
        {
            console.error("last.fm API key is not set");
        }
        else
        {
            const signResp = await fetch(window.config_remoteUrl + `php/getAuthUrl.php?token=${token}&method=auth.getSession`);
            const signature = await signResp.text();
    
            const data = new URLSearchParams();
            data.append("method", "auth.getSession");
            data.append("api_key", apiKey);
            data.append("token", token);
            data.append("api_sig", signature);
    
            const url = `https://ws.audioscrobbler.com/2.0/?format=json`;
    
            const resp = await fetch(url, {
                method: "POST",
                body: data
            });
    
            const json = await resp.json();
    
            if (json.error !== undefined) {
                console.error(`An error happened during last.fm authentification: ${json.message}`);
            }
    
            document.cookie = `lastfm_key=${json.session.key}; max-age=86400; path=/; SameSite=Strict`;
        }
    }

    document.getElementById("lastfmStatus").innerHTML = getCookie("lastfm_key") == undefined ? "Unactive" : "Active";
}