#include "web_server.h"

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "lwip/apps/fs.h"
#include "lwip/apps/httpd.h"
#include "pico/cyw43_arch.h"
#include "pico/time.h"
#include "hardware/watchdog.h"

#include "portal_state.h"
#include "usb_transport.h"
#include "wifi_settings.h"
#include "xsm3_relay.h"

static wifi_settings_t pending_wifi_settings;
static wifi_settings_t current_settings;
static volatile bool wifi_save_pending;
static absolute_time_t wifi_save_time;
static bool setup_mode;

static const char settings_page[] =
"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n"
R"HTML(<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Portal settings</title><style>
:root{color-scheme:dark}body{font:17px/1.5 system-ui;color:#eef5ff;background:radial-gradient(circle at 50% 10%,#253d71,#101b34 40%,#070b16);margin:0;min-height:100vh}.box{width:min(42rem,92vw);margin:7vh auto;padding:2rem;background:#13213bcc;border:1px solid #ffffff20;border-radius:22px;box-shadow:0 18px 60px #0008}h1{margin-top:0}label{display:block;padding:1rem;margin:.75rem 0;border:1px solid #ffffff25;border-radius:14px;cursor:pointer}label:has(input:checked){border-color:#4de8ff;background:#4de8ff18}input{margin-right:.8rem}input[type=text],input[type=password]{display:block;width:100%;margin:.5rem 0 0;padding:.7rem;border:1px solid #ffffff30;border-radius:9px;background:#ffffff0d;color:#eef5ff;font:inherit}small{display:block;color:#95a8c7;margin-left:1.8rem}button,a{font:inherit;padding:.8rem 1rem;border-radius:10px;border:1px solid #ffffff25;color:#eef5ff;background:#ffffff0d;text-decoration:none;cursor:pointer}button{background:#4de8ff;color:#07101f;font-weight:700;margin-right:.6rem}.note{color:#95a8c7}</style></head><body><main class="box"><h1>Portal type</h1><p>Select the console family connected to the Pico USB port.</p><form id="f">
<label><input type="radio" name="type" value="xbox360">Xbox 360<small>24C6:FA01 · 0B 16 transport · XSM3 sidecar required by the console</small></label>
<label><input type="radio" name="type" value="xboxone">Xbox One / Series<small>0E6F:0141 · Xbox GIP transport · no sidecar</small></label>
<label><input type="radio" name="type" value="standard">PlayStation 3/4/5 and Wii U<small>0E6F:0241 · standard HID transport · no sidecar</small></label>
<h2>State JSON verbosity</h2>
<label><input type="radio" name="verbosity" value="none">None<small>Portal state only; no protocol diagnostics</small></label>
<label><input type="radio" name="verbosity" value="xbox-auth">Xbox authentication<small>XSM3 authentication status and traffic on Xbox 360</small></label>
<label><input type="radio" name="verbosity" value="tag-only">Tags only<small>Tag commands, responses, and placement events</small></label>
<label><input type="radio" name="verbosity" value="all">All<small>Xbox authentication, tags, colors, and all other USB messages</small></label>
<h2>Wi-Fi</h2>
<label>Network name<input id="ssid" name="ssid" type="text" maxlength="32"></label>
<label>New password<input name="password" type="password" maxlength="63" autocomplete="new-password"><small>Left blank to keep the current network and password unchanged</small></label>
<p class="note">Saving restarts the Pico so the console can enumerate the new USB identity. Wi-Fi settings are preserved.</p><button>Save and restart</button><a href="/">Cancel</a></form><script>
fetch('/api/settings.json',{cache:'no-store'}).then(r=>r.json()).then(s=>{let t=document.querySelector(`input[name="type"][value="${s.portalType}"]`),v=document.querySelector(`input[name="verbosity"][value="${s.verbosity}"]`);if(t)t.checked=true;if(v)v.checked=true;ssid.value=s.ssid||''});f.onsubmit=async e=>{e.preventDefault();let d=new FormData(f);if(!d.get('type')||!d.get('verbosity'))return;document.open();document.write(await fetch('/api/portal?'+new URLSearchParams(d)).then(r=>r.text()));document.close()};
</script></main></body></html>)HTML";

static const char settings_saved_page[] =
"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nConnection: close\r\n\r\n"
"<!doctype html><meta name=viewport content='width=device-width'><title>Settings saved</title>"
"<style>body{font:18px system-ui;color:#eef5ff;background:#101b34;padding:3rem;max-width:38rem;margin:auto}</style>"
"<h1>Settings saved</h1><p>The Pico is restarting. Restart the console if it does not enumerate the portal automatically.</p>";

static const char wifi_config_page[] =
"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n"
"<!doctype html><meta name=viewport content='width=device-width'><title>Portal Wi-Fi setup</title>"
"<style>body{font:18px system-ui;color:#eef5ff;background:#101b34;padding:3rem;max-width:30rem;margin:auto}"
"form{display:grid;gap:1rem}input,button{font:inherit;padding:.8rem;border-radius:.5rem;border:1px solid #ffffff30}"
"button{color:#07101f;background:#4de8ff;font-weight:700}</style>"
"<h1>Portal Wi-Fi setup</h1><p>Enter a 2.4 GHz network. Settings are stored in the Pico's flash.</p>"
"<form id=f><label>Network name<br><input name=ssid maxlength=32 required></label>"
"<label>Password<br><input name=password type=password maxlength=63></label>"
"<button>Save and restart</button></form><script>f.onsubmit=async e=>{e.preventDefault();let q=new URLSearchParams(new FormData(f));"
"document.open();document.write(await fetch('/api/wifi?'+q).then(r=>r.text()));document.close()}</script>";

static const char wifi_saved_page[] =
"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nConnection: close\r\n\r\n"
"<!doctype html><meta name=viewport content='width=device-width'><title>Wi-Fi saved</title>"
"<style>body{font:18px system-ui;color:#eef5ff;background:#101b34;padding:3rem;max-width:38rem;margin:auto}</style>"
"<h1>Wi-Fi settings saved</h1><p>The portal is restarting and will connect to the new network.</p>";

static const char index_page[] =
"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n"
R"HTML(<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Dimension Toypad</title><style>
:root{color-scheme:dark;--ink:#eef5ff;--muted:#95a8c7;--glass:#13213bcc;--edge:#ffffff20;--cyan:#4de8ff;--pink:#ff4db8}*{box-sizing:border-box}body{margin:0;min-height:100vh;font:15px/1.4 system-ui,sans-serif;color:var(--ink);background:radial-gradient(circle at 50% 18%,#253d71 0,#101b34 35%,#070b16 78%);overflow-x:hidden}body:before{content:"";position:fixed;inset:0;background:linear-gradient(#ffffff05 1px,transparent 1px),linear-gradient(90deg,#ffffff05 1px,transparent 1px);background-size:36px 36px;mask-image:linear-gradient(to bottom,#000,transparent 70%);pointer-events:none}.shell{width:min(1160px,94vw);margin:auto;padding:28px 0 50px}header{display:flex;align-items:end;justify-content:space-between;gap:20px;margin-bottom:22px}h1{margin:0;font-size:clamp(30px,5vw,56px);letter-spacing:-.05em}h1 span{color:var(--cyan);text-shadow:0 0 24px #4de8ff80}.status{color:var(--muted)}.dot{display:inline-block;width:9px;height:9px;border-radius:50%;background:#5cff89;box-shadow:0 0 12px #5cff89;margin-right:7px}.layout{display:grid;grid-template-columns:320px 1fr;gap:22px}.panel{background:var(--glass);border:1px solid var(--edge);border-radius:22px;box-shadow:0 18px 60px #0008;backdrop-filter:blur(14px)}.library{padding:18px;height:650px;display:flex;flex-direction:column}.library h2{margin:0 0 12px}.filters{display:flex;gap:8px;margin-bottom:10px}button,input,select{font:inherit;color:inherit;background:#ffffff0d;border:1px solid var(--edge);border-radius:10px;padding:9px 11px}button{cursor:pointer}button:hover,.active{border-color:var(--cyan);background:#4de8ff18}.search{width:100%;margin-bottom:10px}.cards{overflow:auto;display:grid;gap:8px;padding-right:4px}.card,.tag{border:1px solid var(--edge);border-radius:13px;background:#ffffff0a;padding:10px 12px;cursor:grab}.card{display:grid;grid-template-columns:58px 1fr;gap:11px;align-items:center;min-height:74px}.card img{width:58px;height:58px;object-fit:contain;border-radius:9px;background:#fff;pointer-events:none}.card b{display:block}.card small{display:block;color:var(--muted)}.card .theme{white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.custom{display:grid;grid-template-columns:1fr 76px;gap:8px;margin-top:10px}.deck{padding:24px;min-height:650px;position:relative}.portal{position:relative;height:540px;max-width:720px;margin:auto}.pad{position:absolute;border:2px solid #ffffff30;background:#091122b8;box-shadow:inset 0 0 35px var(--glow),0 0 28px var(--glow);transition:.25s;border-radius:50%;display:flex;align-items:center;justify-content:center;padding:25px}.pad.drag{transform:scale(1.04);border-color:white}.center{--glow:#ffffff18;width:280px;height:280px;left:calc(50% - 140px);top:20px}.left,.right{--glow:#4de8ff22;width:245px;height:245px;top:275px}.left{left:5%}.right{right:5%}.pad h3{position:absolute;top:10px;margin:0;color:#ffffffa8;text-transform:uppercase;letter-spacing:.15em;font-size:11px}.taglist{display:flex;flex-wrap:wrap;justify-content:center;gap:7px;max-width:180px}.tag{padding:7px 9px;cursor:pointer;max-width:150px;text-align:center;background:#182746}.tag:hover{border-color:var(--pink)}.tag small{display:block;color:var(--muted)}.hint{text-align:center;color:var(--muted);margin-top:2px}.clear{position:absolute;right:18px;top:18px}@media(max-width:800px){.layout{grid-template-columns:1fr}.library{height:440px}.deck{min-height:620px}.left{left:0}.right{right:0}}@media(max-width:560px){.deck{padding:10px}.portal{transform:scale(.78);transform-origin:top center;width:125%;margin-left:-12.5%}.deck{min-height:500px}}
.slot{position:absolute;border:2px solid #ffffff30;background:#091122b8;box-shadow:inset 0 0 25px var(--glow),0 0 22px var(--glow);transition:.2s;display:flex;align-items:center;justify-content:center;padding:8px}.slot.drag{transform:scale(1.05);border-color:white}.slot-label{position:absolute;top:7px;margin:0;color:#ffffffa8;text-transform:uppercase;letter-spacing:.15em;font-size:11px}.center-slot{--glow:#ffffff18;width:220px;height:220px;left:calc(50% - 110px);top:12px;border-radius:50%}.side{position:absolute;top:270px;width:270px;height:250px}.left-side{left:3%}.right-side{right:3%}.side .slot{width:125px;height:110px;border-radius:18px}.slot-a{left:0;top:0}.slot-b{left:0;top:125px}.slot-c{left:140px;top:125px}.slot-z{left:140px;top:0}.slot-x{left:0;top:125px}.slot-w{left:140px;top:125px}.slot .taglist{max-width:108px}.slot .tag{font-size:12px;padding:5px 7px}.slot .tag small{font-size:10px}@media(max-width:1000px){.side{transform:scale(.85)}.left-side{left:-2%}.right-side{right:-2%}}@media(max-width:560px){.portal{transform:scale(.72);height:540px}.deck{min-height:440px}}
</style></head><body><main class="shell"><header><div><div class="status"><span class="dot"></span>Pico 2 W · Xbox 360 transport</div><h1>Dimension <span>Deck</span></h1></div><div class="status" id="count">0 / 7 tags</div></header><div class="layout"><aside class="panel library"><h2>Toy collection</h2><div class="filters"><button class="active" data-filter="all">All</button><button data-filter="character">Characters</button><button data-filter="vehicle">Vehicles</button></div><input class="search" id="search" placeholder="Search toys…"><div class="cards" id="cards"></div><div class="custom"><select id="customType"><option value="character">Character</option><option value="vehicle">Vehicle</option></select><input id="customId" type="number" min="0" max="65535" placeholder="ID"></div><small class="status">Drag a toy to one of the seven physical positions.</small></aside><section class="panel deck"><button class="clear" id="clear">Clear all</button><div class="portal"><div class="slot center-slot" data-pad="1" data-position="0"><span class="slot-label">Center</span><div class="taglist"></div></div><div class="side left-side"><div class="slot slot-a" data-pad="2" data-position="1"><span class="slot-label">A</span><div class="taglist"></div></div><div class="slot slot-b" data-pad="2" data-position="2"><span class="slot-label">B</span><div class="taglist"></div></div><div class="slot slot-c" data-pad="2" data-position="3"><span class="slot-label">C</span><div class="taglist"></div></div></div><div class="side right-side"><div class="slot slot-z" data-pad="3" data-position="6"><span class="slot-label">Z</span><div class="taglist"></div></div><div class="slot slot-x" data-pad="3" data-position="4"><span class="slot-label">X</span><div class="taglist"></div></div><div class="slot slot-w" data-pad="3" data-position="5"><span class="slot-label">W</span><div class="taglist"></div></div></div></div><p class="hint">Center: 1 position · Left: A/B/C · Right: X/W/Z · click a tag to remove it</p></section></div></main><script>
document.querySelector('h1 span').textContent='Toypad';
document.head.insertAdjacentHTML('beforeend',`<style>
.card,.slot{touch-action:manipulation}.slot{transition:transform .2s,border-color .2s}.center-slot{width:180px;height:180px;left:calc(50% - 90px)}.side{top:195px;width:280px;height:290px;transform:none}.left-side{left:2%}.right-side{right:2%}.side .slot{width:135px;height:135px}.slot-a{left:0;top:0}.slot-b{left:0;top:145px}.slot-c{left:145px;top:145px}.slot-z{left:145px;top:0}.slot-x{left:0;top:145px}.slot-w{left:145px;top:145px}.portal{height:490px}.deck{min-height:590px}
@media(max-width:560px){.deck{padding:10px;min-height:430px}.portal{transform:none;width:calc(100% + 28px);margin-left:-14px;height:340px}.side{display:contents}.center-slot{width:145px;height:145px;left:calc(50% - 72.5px);top:0}.side .slot{width:86px;height:86px}.slot-a{left:0;top:150px}.slot-b{left:0;top:245px}.slot-c{left:95px;top:245px}.slot-z{left:auto;right:0;top:150px}.slot-x{left:auto;right:95px;top:245px}.slot-w{left:auto;right:0;top:245px}}
</style>`);
document.querySelector('.library>.status').textContent='Tap a toy, then tap a position. You can also drag it on desktop.';
const toys=[
['character',1,'Batman','DC Comics'],['character',2,'Gandalf','Lord of the Rings'],['character',3,'Wyldstyle','The Lego Movie'],['character',4,'Aquaman','DC Comics'],['character',5,'Bad Cop','The Lego Movie'],['character',6,'Bane','DC Comics'],['character',7,'Bart Simpson','The Simpsons'],['character',8,'Benny','The Lego Movie'],['character',9,'Chell','Portal 2'],['character',10,'Cole','Lego Ninjago'],['character',11,'Cragger','Lego Legends of Chima'],['character',12,'Cyborg','DC Comics','11-Cyborg.jpg'],['character',13,'Cyberman','Doctor Who'],['character',14,'Doc Brown','Back to the Future'],['character',15,'The Doctor','Doctor Who'],['character',16,'Emmet','The Lego Movie'],['character',17,'Eris','Lego Legends of Chima'],['character',18,'Gimli','Lord of the Rings'],['character',19,'Gollum','Lord of the Rings'],['character',20,'Harley Quinn','DC Comics'],['character',21,'Homer Simpson','The Simpsons'],['character',22,'Jay','Lego Ninjago'],['character',23,'Joker','DC Comics'],['character',24,'Kai','Lego Ninjago'],['character',25,'ACU Trooper','Jurassic World'],['character',26,'Gamer Kid','Midway Arcade'],['character',27,'Krusty','The Simpsons'],['character',28,'Laval','Lego Legends of Chima'],['character',29,'Legolas','Lord of the Rings'],['character',30,'Lloyd','Lego Ninjago'],['character',31,'Marty McFly','Back to the Future'],['character',32,'Nya','Lego Ninjago'],['character',33,'Owen','Jurassic World'],['character',34,'Peter Venkman','Ghostbusters'],['character',35,'Slimer','Ghostbusters'],['character',36,'Scooby Doo','Scooby-Doo'],['character',37,'Sensei Wu','Lego Ninjago'],['character',38,'Shaggy','Scooby-Doo'],['character',39,'Stay Puft','Ghostbusters'],['character',40,'Superman','DC Comics'],['character',41,'Unikitty','The Lego Movie'],['character',42,'Wicked Witch','Wizard of Oz'],['character',43,'Wonder Woman','DC Comics'],['character',44,'Zane','Lego Ninjago'],['character',45,'Green Arrow','DC Comics'],['character',46,'Supergirl','DC Comics'],['character',47,'Abby Yates','Ghostbusters 2016'],['character',48,'Finn','Adventure Time'],['character',49,'Ethan Hunt','Mission: Impossible'],['character',50,'Lumpy Space Princess','Adventure Time'],['character',51,'Jake the Dog','Adventure Time'],['character',52,'Harry Potter','Harry Potter'],['character',53,'Lord Voldemort','Harry Potter'],['character',54,'Michael Knight','Knight Rider'],['character',55,'B.A.Baracus','The A-Team'],['character',56,'Newt Scamander','Fantastic Beasts'],['character',57,'Sonic the Hedgehog','Sonic the Hedgehog'],['character',59,'Gizmo','Gremlins'],['character',60,'Stripe','Gremlins'],['character',61,'E.T.','E.T. the Extra-Terrestrial'],['character',62,'Tina Goldstein','Fantastic Beasts'],['character',63,'Marceline Abadeer','Adventure Time'],['character',64,'Batgirl','The LEGO Batman Movie'],['character',65,'Robin (Lego Movie)','The LEGO Batman Movie','65-Robin (Lego Movie).png'],['character',66,'Sloth','The Goonies'],['character',67,'Hermione Granger','Harry Potter'],['character',68,'Chase McCain','LEGO City: Undercover'],['character',69,'Excalibur Batman','The LEGO Batman Movie'],['character',70,'Raven','Teen Titans Go!'],['character',71,'Beast Boy','Teen Titans Go!'],['character',72,'Beetlejuice','Beetlejuice'],['character',74,'Blossom','The Powerpuff Girls'],['character',75,'Bubbles','The Powerpuff Girls'],['character',76,'Buttercup','The Powerpuff Girls'],['character',77,'Starfire','Teen Titans Go!'],['character',81,'Supergirl Red Lantern','DC Comics'],
['vehicle',1000,'Police Car','The Lego Movie'],['vehicle',1006,'Batmobile','DC Comics'],['vehicle',1012,'DeLorean Time Machine','Back to the Future'],['vehicle',1015,'Hoverboard','Back to the Future'],['vehicle',1030,'TARDIS','Doctor Who'],['vehicle',1066,'Mystery Machine','Scooby-Doo'],['vehicle',1081,'Companion Cube','Portal 2'],['vehicle',1120,'Ecto-1','Ghostbusters'],['vehicle',1123,'Ghost Trap','Ghostbusters'],['vehicle',1158,'Arcade Machine','Midway Arcade'],['vehicle',1173,'BMO','Adventure Time'],['vehicle',1212,'IMF Scrambler','Mission: Impossible'],['vehicle',1218,'Sonic Speedster','Sonic the Hedgehog'],['vehicle',1224,'K.I.T.T.','Knight Rider'],['vehicle',1242,'Buckbeak','Harry Potter']];
const enhancements={
'character:46':[[46,'Supergirl'],[81,'Supergirl Red Lantern']],
'vehicle:1000':[[1000,'Police Car'],[1001,'Aerial Squad Car'],[1002,'Missile Striker']],
'vehicle:1006':[[1006,'Batmobile'],[1007,'Batblaster'],[1008,'Sonic Batray']],
'vehicle:1012':[[1012,'DeLorean Time Machine'],[1013,'Ultra Time Machine'],[1014,'Electric Time Machine']],
'vehicle:1015':[[1015,'Hoverboard'],[1016,'Cyclone Board'],[1017,'Ultimate Hoverjet']],
'vehicle:1030':[[1030,'TARDIS'],[1031,'Laser-Pulse TARDIS'],[1032,'Energy-Burst TARDIS']],
'vehicle:1066':[[1066,'Mystery Machine'],[1067,'Mystery Tow'],[1068,'Mystery Monster']],
'vehicle:1081':[[1081,'Companion Cube'],[1082,'Laser Deflector'],[1083,'Gold Heart Emitter']],
'vehicle:1120':[[1120,'Ecto-1'],[1121,'Ecto-1 Blaster'],[1122,'Ecto-1 Water Diver']],
'vehicle:1123':[[1123,'Ghost Trap'],[1124,"Ghost Stun'n'Trap"],[1125,'Proton Zapper']],
'vehicle:1158':[[1158,'Arcade Machine'],[1159,'8-bit Shooter'],[1160,'The Pixelator']],
'vehicle:1173':[[1173,'BMO'],[1174,'DOGMO'],[1175,'SNAKEMO']],
'vehicle:1212':[[1212,'IMF Scrambler'],[1213,'Shock Cycle'],[1214,'IMF Covert Jet']],
'vehicle:1218':[[1218,'Sonic Speedster'],[1219,'Blue Typhoon'],[1220,'Moto Bug']],
'vehicle:1224':[[1224,'K.I.T.T.'],[1225,'Goliath Armored Semi'],[1226,'K.I.T.T. Jet']],
'vehicle:1242':[[1242,'Buckbeak'],[1243,'Giant Owl'],[1244,'Fierce Falcon']]};
let filter='all',dragged=null,chosen=null,state={tags:[],colors:[[0,0,0],[0,0,0],[0,0,0]]};const cards=document.querySelector('#cards'),selected={};const variantIds=new Set(Object.entries(enhancements).flatMap(([key,v])=>v.slice(1).map(x=>`${key.split(':')[0]}:${x[0]}`)));const allToys=()=>toys.flatMap(t=>(enhancements[`${t[0]}:${t[1]}`]||[[t[1],t[2]]]).map(v=>[t[0],v[0],v[1],t[3]]));const nameOf=(kind,id)=>allToys().find(t=>t[0]===kind&&t[1]===id)?.[2]||`${kind} ${id}`;
const imageRoot='https://raw.githubusercontent.com/wiki/Ellerbach/LegoDimensions/.attachments/';
function imageName(t){return t[4]||`${t[1]}-${t[2]}.jpg`}
function drawLibrary(){let q=document.querySelector('#search').value.trim().toLowerCase();let list=toys.filter(t=>!variantIds.has(`${t[0]}:${t[1]}`)&&(filter==='all'||t[0]===filter)&&`${t[2]} ${t[3]} ${(enhancements[`${t[0]}:${t[1]}`]||[]).map(v=>v[1]).join(' ')}`.toLowerCase().includes(q));let rawId=document.querySelector('#customId').value,cid=Number(rawId),ct=document.querySelector('#customType').value;if(rawId!==''&&Number.isInteger(cid)&&cid>=0)list.unshift([ct,cid,`Custom ${ct} ${cid}`,'Custom']);cards.innerHTML=list.map(t=>{let key=`${t[0]}:${t[1]}`,forms=enhancements[key],level=forms?selected[key]||0:0,v=forms?forms[level]:[t[1],t[2]],shown=[t[0],v[0],v[1],t[3]],active=chosen&&chosen.kind===t[0]&&chosen.base===t[1];return `<div class="card${active?' active':''}" draggable="true" data-kind="${t[0]}" data-base="${t[1]}" data-id="${v[0]}">${t[3]==='Custom'?'':`<img loading="lazy" src="${imageRoot}${encodeURIComponent(imageName(shown))}" alt="" onerror="this.style.visibility='hidden'">`}<div><b>${v[1]}</b><small>${t[0]} · ID ${v[0]}${forms?` · ${t[0]==='vehicle'?`build ${level+1}`:`form ${level+1}`}/${forms.length}`:''}</small><small class="theme">${active?'Selected · tap a position':forms?'Tap to select · tap again to change':'Tap to select'}</small></div></div>`}).join('');cards.querySelectorAll('.card').forEach(c=>{c.onclick=()=>{let key=`${c.dataset.kind}:${c.dataset.base}`,forms=enhancements[key],same=chosen&&chosen.kind===c.dataset.kind&&chosen.base===+c.dataset.base;if(same&&forms)selected[key]=((selected[key]||0)+1)%forms.length;chosen={kind:c.dataset.kind,base:+c.dataset.base,id:+c.dataset.id};if(same&&forms)chosen.id=forms[selected[key]][0];drawLibrary()};c.ondragstart=()=>dragged={kind:c.dataset.kind,id:+c.dataset.id}});}
async function api(path){await fetch(path,{cache:'no-store'});await refresh()}async function refresh(){try{state=await fetch('/api/state.json',{cache:'no-store'}).then(r=>r.json());render();renderMode()}catch(e){document.querySelector('.dot').style.background='#ff566d'}}
const modeNames={xbox360:'Xbox 360',xboxone:'Xbox One / Series',standard:'PlayStation / Wii U'};const settingsLink=document.createElement('a');settingsLink.href='/settings.html';settingsLink.textContent='Settings';settingsLink.style.cssText='color:var(--cyan);margin-left:14px';document.querySelector('header').append(settingsLink);function renderMode(){let mode=state.portalType||'xbox360',label=modeNames[mode]||mode,header=document.querySelector('header .status');header.lastChild.nodeValue=`Pico 2 W · ${label} transport`}
function render(){let auth=state.xsm3,summary=`${state.tags.length} / 7 tags`;if(auth)summary+=` · Auth ${auth.connected?'ready':'offline'} · ${auth.responses||0}/${auth.requests||0}`;document.querySelector('#count').textContent=summary;document.querySelectorAll('.slot').forEach(s=>{let pad=+s.dataset.pad,c=state.colors[pad-1]||[0,0,0],rgb=`${c[0]},${c[1]},${c[2]}`;s.style.setProperty('--glow',`rgba(${rgb},.85)`);s.style.background=`radial-gradient(circle,rgba(${rgb},.7),rgba(${rgb},.28) 55%,#091122d9 100%)`;s.style.borderColor=`rgba(${rgb},.9)`;s.querySelector('.taglist').innerHTML=state.tags.filter(t=>t.position===+s.dataset.position).map(t=>`<div class="tag" data-index="${t.index}">${nameOf(t.kind,t.id)}<small>${t.kind} · ${t.index}</small></div>`).join('');s.querySelectorAll('.tag').forEach(x=>x.onclick=e=>{e.stopPropagation();api(`/api/remove?index=${x.dataset.index}`)})})}
document.querySelectorAll('[data-filter]').forEach(b=>b.onclick=()=>{document.querySelectorAll('[data-filter]').forEach(x=>x.classList.remove('active'));b.classList.add('active');filter=b.dataset.filter;drawLibrary()});document.querySelector('#search').oninput=drawLibrary;document.querySelector('#customId').oninput=drawLibrary;document.querySelector('#customType').onchange=drawLibrary;document.querySelectorAll('.slot').forEach(s=>{s.ondragover=e=>{e.preventDefault();s.classList.add('drag')};s.ondragleave=()=>s.classList.remove('drag');s.ondrop=e=>{e.preventDefault();s.classList.remove('drag');if(dragged)api(`/api/place?pad=${s.dataset.pad}&position=${s.dataset.position}&kind=${dragged.kind==='vehicle'?1:0}&id=${dragged.id}`)};s.onclick=async()=>{if(!chosen||state.tags.some(t=>t.position===+s.dataset.position))return;await api(`/api/place?pad=${s.dataset.pad}&position=${s.dataset.position}&kind=${chosen.kind==='vehicle'?1:0}&id=${chosen.id}`);chosen=null;drawLibrary()}});document.querySelector('#clear').onclick=()=>Promise.all([1,2,3].map(p=>fetch(`/api/clear?pad=${p}`))).then(refresh);drawLibrary();refresh();setInterval(refresh,100);
</script></body></html>)HTML";

#define JSON_BUFFER_SIZE 32768

static char json_buffers[2][JSON_BUFFER_SIZE];
static unsigned json_buffer_index;

static size_t append_json(char *buffer, size_t used, const char *format, ...) {
    if (used >= JSON_BUFFER_SIZE - 1) return JSON_BUFFER_SIZE - 1;
    va_list args;
    va_start(args, format);
    int written = vsnprintf(buffer + used, JSON_BUFFER_SIZE - used, format, args);
    va_end(args);
    if (written < 0) return used;
    if ((size_t)written >= JSON_BUFFER_SIZE - used) return JSON_BUFFER_SIZE - 1;
    return used + (size_t)written;
}

static size_t append_json_string(char *buffer, size_t used, const char *value) {
    used = append_json(buffer, used, "\"");
    for (const unsigned char *p = (const unsigned char *)value; *p; p++) {
        if (*p == '\"' || *p == '\\') used = append_json(buffer, used, "\\%c", *p);
        else if (*p < 0x20) used = append_json(buffer, used, "\\u%04x", *p);
        else used = append_json(buffer, used, "%c", *p);
    }
    return append_json(buffer, used, "\"");
}

static int parameter_int(int count, char *names[], char *values[], const char *wanted, int fallback) {
    for (int i = 0; i < count; i++) {
        if (strcmp(names[i], wanted) == 0) {
            return (int)strtol(values[i], NULL, 10);
        }
    }
    return fallback;
}

static const char *api_handler(int index, int count, char *names[], char *values[]) {
    int pad = parameter_int(count, names, values, "pad", 0);
    if (index == 0) {
        int position = parameter_int(count, names, values, "position", 255);
        int kind = parameter_int(count, names, values, "kind", 0);
        int id = parameter_int(count, names, values, "id", 0);
        portal_state_place((uint8_t)pad, (uint8_t)position, (tag_kind_t)kind, (uint16_t)id);
    } else if (index == 1) {
        portal_state_remove((uint8_t)parameter_int(count, names, values, "index", 255));
    } else if (index == 2) {
        portal_state_remove_pad((uint8_t)pad);
    }
    return "/api/state.json";
}

static const char *wifi_handler(int index, int count, char *names[], char *values[]) {
    (void)index;
    const char *ssid = NULL;
    const char *password = "";
    for (int i = 0; i < count; i++) {
        if (strcmp(names[i], "ssid") == 0) {
            ssid = values[i];
        } else if (strcmp(names[i], "password") == 0) {
            password = values[i];
        }
    }
    if (ssid == NULL || strlen(ssid) == 0 || strlen(ssid) > WIFI_SETTINGS_SSID_MAX ||
        strlen(password) > WIFI_SETTINGS_PASSWORD_MAX) {
        return "/";
    }
    pending_wifi_settings = current_settings;
    snprintf(pending_wifi_settings.ssid, sizeof(pending_wifi_settings.ssid), "%s", ssid);
    snprintf(pending_wifi_settings.password, sizeof(pending_wifi_settings.password), "%s", password);
    wifi_save_time = make_timeout_time_ms(1500);
    wifi_save_pending = true;
    return "/wifi-saved.html";
}

static const char *portal_handler(int index, int count, char *names[], char *values[]) {
    (void)index;
    const char *type = NULL;
    const char *verbosity = NULL;
    const char *ssid = NULL;
    const char *password = NULL;
    for (int i = 0; i < count; i++) {
        if (strcmp(names[i], "type") == 0) type = values[i];
        else if (strcmp(names[i], "verbosity") == 0) verbosity = values[i];
        else if (strcmp(names[i], "ssid") == 0) ssid = values[i];
        else if (strcmp(names[i], "password") == 0) password = values[i];
    }
    portal_usb_variant_t variant;
    if (type != NULL && strcmp(type, "xbox360") == 0) {
        variant = PORTAL_USB_XBOX_360;
    } else if (type != NULL && strcmp(type, "xboxone") == 0) {
        variant = PORTAL_USB_XBOX_ONE;
    } else if (type != NULL && strcmp(type, "standard") == 0) {
        variant = PORTAL_USB_STANDARD;
    } else {
        return "/settings.html";
    }
    state_verbosity_t state_verbosity;
    if (verbosity != NULL && strcmp(verbosity, "none") == 0) {
        state_verbosity = STATE_VERBOSITY_NONE;
    } else if (verbosity != NULL && strcmp(verbosity, "xbox-auth") == 0) {
        state_verbosity = STATE_VERBOSITY_XBOX_AUTH;
    } else if (verbosity != NULL && strcmp(verbosity, "tag-only") == 0) {
        state_verbosity = STATE_VERBOSITY_TAG_ONLY;
    } else if (verbosity != NULL && strcmp(verbosity, "all") == 0) {
        state_verbosity = STATE_VERBOSITY_ALL;
    } else {
        return "/settings.html";
    }
    pending_wifi_settings = current_settings;
    pending_wifi_settings.portal_variant = variant;
    pending_wifi_settings.state_verbosity = state_verbosity;
    if (password != NULL && password[0] != '\0') {
        if (ssid == NULL || ssid[0] == '\0' || strlen(ssid) > WIFI_SETTINGS_SSID_MAX ||
                strlen(password) > WIFI_SETTINGS_PASSWORD_MAX) {
            return "/settings.html";
        }
        snprintf(pending_wifi_settings.ssid, sizeof(pending_wifi_settings.ssid), "%s", ssid);
        snprintf(pending_wifi_settings.password, sizeof(pending_wifi_settings.password), "%s", password);
    }
    wifi_save_time = make_timeout_time_ms(1500);
    wifi_save_pending = true;
    return "/settings-saved.html";
}

static const tCGI handlers[] = {
    {"/api/place", api_handler},
    {"/api/remove", api_handler},
    {"/api/clear", api_handler},
    {"/api/wifi", wifi_handler},
    {"/api/portal", portal_handler},
};

static const char *verbosity_name(state_verbosity_t verbosity) {
    static const char *names[] = {"none", "xbox-auth", "tag-only", "all"};
    return verbosity <= STATE_VERBOSITY_ALL ? names[verbosity] : names[0];
}

static const char *build_settings_json(size_t *length) {
    char *buffer = json_buffers[json_buffer_index++ & 1u];
    size_t used = append_json(buffer, 0,
        "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n{\"portalType\":\"%s\",\"verbosity\":\"%s\",\"ssid\":",
        usb_transport_variant_name(current_settings.portal_variant),
        verbosity_name(current_settings.state_verbosity));
    used = append_json_string(buffer, used, current_settings.ssid);
    used = append_json(buffer, used, "}");
    *length = used;
    return buffer;
}

static const uint8_t *trace_lego_frame(const usb_trace_entry_t *entry, uint8_t *length) {
    if (entry->length >= 4 && entry->data[0] == 0x0b && entry->data[1] == 0x16) {
        *length = entry->length - 2;
        return entry->data + 2;
    }
    if (entry->length >= 8 && entry->data[0] == 0x21 && entry->data[3] == 0x20) {
        *length = entry->length - 4;
        return entry->data + 4;
    }
    *length = entry->length;
    return entry->data;
}

static bool is_tag_command(const usb_trace_entry_t *entry) {
    uint8_t length;
    const uint8_t *frame = trace_lego_frame(entry, &length);
    if (entry->portal_to_xbox || length < 3 || frame[0] != 0x55) return false;
    return frame[2] == 0xd0 || frame[2] == 0xd2 || frame[2] == 0xd3 ||
        frame[2] == 0xd4 || frame[2] == 0xe1 || frame[2] == 0xe5;
}

static bool is_tag_event(const usb_trace_entry_t *entry) {
    uint8_t length;
    const uint8_t *frame = trace_lego_frame(entry, &length);
    return entry->portal_to_xbox && length > 0 && frame[0] == 0x56;
}

static const char *build_json(size_t *length) {
    static portal_snapshot_t snapshot;
    static xsm3_relay_status_t relay;
    static xsm3_trace_snapshot_t trace;
    static usb_transport_status_t usb;
    state_verbosity_t verbosity = current_settings.state_verbosity;
    bool include_auth = usb_transport_variant() == PORTAL_USB_XBOX_360 &&
        (verbosity == STATE_VERBOSITY_XBOX_AUTH || verbosity == STATE_VERBOSITY_ALL);
    bool include_usb = verbosity == STATE_VERBOSITY_TAG_ONLY || verbosity == STATE_VERBOSITY_ALL;
    portal_state_snapshot(&snapshot);
    if (include_auth) {
        xsm3_relay_get_status(&relay);
        xsm3_relay_get_trace(&trace);
    } else {
        memset(&relay, 0, sizeof(relay));
        memset(&trace, 0, sizeof(trace));
    }
    if (include_usb) usb_transport_get_status(&usb);
    char *buffer = json_buffers[json_buffer_index++ & 1u];
    size_t used = append_json(buffer, 0,
        "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n{\"portalType\":\"%s\",\"verbosity\":\"%s\",\"nfc\":%s",
        usb_transport_variant_name(usb_transport_variant()),
        verbosity_name(verbosity), snapshot.nfc_enabled ? "true" : "false");
    if (include_auth) {
        used = append_json(buffer, used,
        ",\"xsm3\":{\"connected\":%s,\"requests\":%lu,\"responses\":%lu,\"errors\":%lu,\"timeouts\":%lu,\"unsupportedRequests\":%lu,\"lastRequest\":%u,\"lastInterface\":%u,\"lastUnsupported\":{\"bmRequestType\":%u,\"bRequest\":%u,\"wValue\":%u,\"wIndex\":%u,\"wLength\":%u},\"traffic\":[",
        relay.sidecar_connected ? "true" : "false",
        (unsigned long)relay.requests, (unsigned long)relay.responses,
        (unsigned long)relay.errors, (unsigned long)relay.timeouts,
        (unsigned long)relay.unsupported_requests,
        relay.last_request, relay.last_interface,
        relay.last_unsupported_bm_request_type,
        relay.last_unsupported_request, relay.last_unsupported_value,
        relay.last_unsupported_index, relay.last_unsupported_length);
        static const char *event_names[] = {
            "control_in_request", "control_in_response",
            "control_out_request", "control_out_ack",
        };
        static const char *status_names[] = {"sent", "ok", "error", "timeout"};
        for (uint8_t i = 0; i < trace.count; i++) {
            const xsm3_trace_entry_t *entry = &trace.entries[i];
            used = append_json(buffer, used,
            "%s{\"sequence\":%lu,\"transaction\":%lu,\"timestampMs\":%lu,\"type\":\"%s\",\"direction\":\"%s\",\"status\":\"%s\",\"statusCode\":%d,\"bmRequestType\":%u,\"bRequest\":%u,\"wValue\":%u,\"wIndex\":%u,\"wLength\":%u,\"payload\":\"",
            i == 0 ? "" : ",", (unsigned long)entry->sequence,
            (unsigned long)entry->transaction, (unsigned long)entry->timestamp_ms,
            event_names[entry->event],
            entry->event == XSM3_TRACE_CONTROL_IN_REQUEST ||
                entry->event == XSM3_TRACE_CONTROL_OUT_REQUEST ?
                "xbox_to_portal" : "portal_to_xbox",
            status_names[entry->status], entry->status_code,
            entry->bm_request_type, entry->request, entry->value,
            entry->index, entry->requested_length);
            for (uint8_t j = 0; j < entry->data_length; j++) {
                used = append_json(buffer, used, "%02x", entry->data[j]);
            }
            used = append_json(buffer, used, "\"}");
        }
        used = append_json(buffer, used, "]}");
    }
    if (include_usb) {
        used = append_json(buffer, used, ",\"usb\":{\"mounted\":%s", usb.mounted ? "true" : "false");
        if (verbosity == STATE_VERBOSITY_ALL) {
            used = append_json(buffer, used,
                ",\"rxTransfers\":%lu,\"txTransfers\":%lu,\"txFailures\":%lu,\"xinputCommands\":%lu,\"legoCommands\":%lu,\"wakeCommands\":%lu,\"lastRx\":\"",
                (unsigned long)usb.rx_transfers, (unsigned long)usb.tx_transfers,
                (unsigned long)usb.tx_failures, (unsigned long)usb.xinput_commands,
                (unsigned long)usb.lego_commands, (unsigned long)usb.wake_commands);
            for (uint8_t i = 0; i < usb.last_rx_length; i++) used = append_json(buffer, used, "%02x", usb.last_rx[i]);
            used = append_json(buffer, used, "\",\"lastTx\":\"");
            for (uint8_t i = 0; i < usb.last_tx_length; i++) used = append_json(buffer, used, "%02x", usb.last_tx[i]);
            used = append_json(buffer, used, "\"");
        }
        used = append_json(buffer, used, ",\"traffic\":[");
        bool first_usb = true;
        bool awaiting_tag_response = false;
        for (uint8_t i = 0; i < usb.trace_count; i++) {
            const usb_trace_entry_t *entry = &usb.trace[i];
            bool include = verbosity == STATE_VERBOSITY_ALL;
            if (verbosity == STATE_VERBOSITY_TAG_ONLY) {
                if (is_tag_command(entry)) {
                    include = true;
                    awaiting_tag_response = true;
                } else if (is_tag_event(entry)) {
                    include = true;
                } else if (entry->portal_to_xbox && awaiting_tag_response) {
                    uint8_t frame_length;
                    const uint8_t *frame = trace_lego_frame(entry, &frame_length);
                    include = frame_length > 0 && frame[0] == 0x55;
                    if (include) awaiting_tag_response = false;
                } else if (!entry->portal_to_xbox) {
                    awaiting_tag_response = false;
                }
            }
            if (!include) continue;
            used = append_json(buffer, used,
            "%s{\"timestampMs\":%lu,\"direction\":\"%s\",\"payload\":\"",
            first_usb ? "" : ",", (unsigned long)entry->timestamp_ms,
            entry->portal_to_xbox ? "portal_to_xbox" : "xbox_to_portal");
            for (uint8_t j = 0; j < entry->length; j++) used = append_json(buffer, used, "%02x", entry->data[j]);
            used = append_json(buffer, used, "\"}");
            first_usb = false;
        }
        used = append_json(buffer, used, "]}");
    }
    used = append_json(buffer, used,
        ",\"colors\":[[%u,%u,%u],[%u,%u,%u],[%u,%u,%u]],\"tags\":[",
        snapshot.colors[0][0], snapshot.colors[0][1], snapshot.colors[0][2],
        snapshot.colors[1][0], snapshot.colors[1][1], snapshot.colors[1][2],
        snapshot.colors[2][0], snapshot.colors[2][1], snapshot.colors[2][2]);
    bool first = true;
    for (int i = 0; i < PORTAL_MAX_TAGS; i++) {
        const portal_tag_t *tag = &snapshot.tags[i];
        if (!tag->present) {
            continue;
        }
        used = append_json(buffer, used,
            "%s{\"pad\":%u,\"position\":%u,\"index\":%u,\"kind\":\"%s\",\"id\":%u,\"uid\":\"%02X%02X%02X%02X%02X%02X%02X\"}",
            first ? "" : ",", tag->pad, tag->position, tag->index,
            tag->kind == TAG_VEHICLE ? "vehicle" : "character", tag->id,
            tag->uid[0], tag->uid[1], tag->uid[2], tag->uid[3], tag->uid[4], tag->uid[5], tag->uid[6]);
        first = false;
    }
    used = append_json(buffer, used, "]}");
    *length = used;
    return buffer;
}

extern "C" int fs_open_custom(struct fs_file *file, const char *name) {
    memset(file, 0, sizeof(*file));
    if (strcmp(name, "/") == 0 || strcmp(name, "/index.html") == 0) {
        file->data = setup_mode ? wifi_config_page : index_page;
        file->len = setup_mode ? sizeof(wifi_config_page) - 1 : sizeof(index_page) - 1;
    } else if (strcmp(name, "/wifi.html") == 0) {
        file->data = wifi_config_page;
        file->len = sizeof(wifi_config_page) - 1;
    } else if (strcmp(name, "/api/state.json") == 0) {
        size_t length;
        file->data = build_json(&length);
        file->len = (int)length;
    } else if (strcmp(name, "/api/settings.json") == 0) {
        size_t length;
        file->data = build_settings_json(&length);
        file->len = (int)length;
    } else if (strcmp(name, "/wifi-saved.html") == 0) {
        file->data = wifi_saved_page;
        file->len = sizeof(wifi_saved_page) - 1;
    } else if (strcmp(name, "/settings.html") == 0) {
        file->data = settings_page;
        file->len = sizeof(settings_page) - 1;
    } else if (strcmp(name, "/settings-saved.html") == 0) {
        file->data = settings_saved_page;
        file->len = sizeof(settings_saved_page) - 1;
    } else {
        return 0;
    }
    file->index = file->len;
    file->flags = FS_FILE_FLAGS_HEADER_INCLUDED;
    return 1;
}

extern "C" void fs_close_custom(struct fs_file *file) {
    (void)file;
}

extern "C" void web_server_init(void) {
    cyw43_arch_lwip_begin();
    httpd_init();
    http_set_cgi_handlers(handlers, sizeof(handlers) / sizeof(handlers[0]));
    cyw43_arch_lwip_end();
}

extern "C" void web_server_set_setup_mode(int enabled) {
    setup_mode = enabled != 0;
}

extern "C" void web_server_set_settings(const wifi_settings_t *settings) {
    current_settings = *settings;
}

extern "C" void web_server_task(void) {
    if (!wifi_save_pending || !time_reached(wifi_save_time)) {
        return;
    }
    wifi_save_pending = false;
    if (wifi_settings_save(&pending_wifi_settings)) {
        printf("Settings saved; restarting.\n");
        watchdog_reboot(0, 0, 100);
    } else {
        printf("ERROR: Failed to save Wi-Fi settings.\n");
    }
}
