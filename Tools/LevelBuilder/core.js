(function (root) {
  'use strict';
  const clone = v => JSON.parse(JSON.stringify(v));
  const overlap = (a,b) => a.x < b.x+b.w && b.x < a.x+a.w && a.y < b.y+b.h && b.y < a.y+a.h;
  const ordinal = (a,b) => a < b ? -1 : a > b ? 1 : 0;
  function normalize(input) {
    if (!input || typeof input !== 'object' || Array.isArray(input) || !Array.isArray(input.stacks) || !Array.isArray(input.targets)) throw Error('Cần JSON CardSlot có stacks và targets.');
    const l = clone(input);
    l.revision ??= 1; l.seed ??= 0; l.n_slots ??= 3; l.buffer_capacity ??= 12;
    l.stacks.forEach(s => { s.fan ??= false; s.spread ??= true; s.spread_direction ??= 0; s.spread_angle ??= 0; });
    return l;
  }
  function validate(l) {
    const errors = [], ids = new Set(), cards = Array(8).fill(0), targets = Array(8).fill(0);
    const range = (v,a,b,label) => { if (!Number.isInteger(v) || v<a || v>b) errors.push(`${label}: cần số nguyên ${a}–${b}`); };
    const color = c => /^color_[0-7]$/.test(c);
    if (typeof l.id !== 'string' || !l.id.trim()) errors.push('Thiếu ID màn');
    range(l.n_slots,2,4,'Số cọc đích'); range(l.buffer_capacity,1,60,'Buffer');
    range(l.stacks.length,2,30,'Số cọc bài');
    if (!l.targets.length) errors.push('Chưa có đích');
    l.targets.forEach(t => { range(t.capacity,2,36,'Sức chứa đích'); if (!color(t.color)) errors.push('Màu đích không hợp lệ'); else targets[+t.color.slice(6)] += t.capacity; });
    l.stacks.forEach(s => {
      if (typeof s.id !== 'string' || !s.id || ids.has(s.id)) errors.push('ID cọc thiếu hoặc trùng: '+s.id); ids.add(s.id);
      range(s.count,1,48,s.id+' / số bài'); range(s.layer,0,4,s.id+' / tầng');
      range(s.spread_direction,-1,1,s.id+' / chiều'); range(s.spread_angle,-180,180,s.id+' / góc');
      for (const k of ['x','y','w','h']) if (!Number.isInteger(s[k]) || ((k==='w'||k==='h') && s[k]<=0)) errors.push(s.id+' / '+k+' không hợp lệ');
      if (typeof s.fan !== 'boolean' || typeof s.spread !== 'boolean') errors.push(s.id+' / fan và spread phải là boolean');
      if (!color(s.color)) errors.push(s.id+' / màu không hợp lệ'); else cards[+s.color.slice(6)] += s.count;
      if (s.on_stack) { const p = l.stacks.find(p=>p.id===s.on_stack); if (!p || p.layer>=s.layer || !overlap(s,p) || p.spread_angle!==s.spread_angle) errors.push(s.id+' / cọc đỡ phải ở tầng dưới, giao vùng và cùng hướng xòe'); }
    });
    for (let c=0;c<8;c++) if (cards[c]!==targets[c]) errors.push(`color_${c}: ${cards[c]} bài / ${targets[c]} sức chứa đích`);
    if (targets.filter(n=>n>0).length<2) errors.push('Cần ít nhất 2 màu');
    l.stacks.forEach((a,i)=>l.stacks.slice(i+1).forEach(b=>{if(a.layer===b.layer&&overlap(a,b)) errors.push(`${a.id} và ${b.id} giao nhau cùng tầng`);}));
    return errors;
  }
  function resolve(l) {
    const ss=l.stacks, order=ss.map((_,i)=>i).sort((a,b)=>ss[a].layer-ss[b].layer || ordinal(ss[a].id,ss[b].id)), next=ss.map(()=>0), out=[];
    for (const i of order) {
      const s=ss[i]; let candidates=order.filter(j=>ss[j].layer<s.layer && overlap(s,ss[j]) && ss[j].spread_angle===s.spread_angle);
      const area=p=>(Math.min(s.x+s.w,p.x+p.w)-Math.max(s.x,p.x))*(Math.min(s.y+s.h,p.y+p.h)-Math.max(s.y,p.y));
      const dist=p=>(s.x*2+s.w-p.x*2-p.w)**2+(s.y*2+s.h-p.y*2-p.h)**2;
      candidates.sort((a,b)=>ss[b].layer-ss[a].layer || area(ss[b])-area(ss[a]) || dist(ss[a])-dist(ss[b]) || ordinal(ss[a].id,ss[b].id));
      const support=candidates.find(j=>ss[j].id===s.on_stack) ?? candidates[0] ?? -1;
      const root=support<0?i:out[support].root; out[i]={root,support,offset:next[root]}; next[root]+=s.count;
    }
    const centres=out.flatMap((p,i)=>p.root===i?[ss[i].x*2+ss[i].w]:[]), mid=Math.min(...centres)+Math.max(...centres);
    out.forEach(p=>{const r=ss[p.root];p.direction=r.spread_direction || (r.x*4+r.w*2<mid?-1:1);}); return out;
  }
  function geometry(l,packed=true) {
    const placements=resolve(l), groups=new Map(), poses=[];
    l.stacks.forEach((s,i)=>{
      const p=placements[i],r=l.stacks[p.root], cards=[];
      for(let b=0;b<Math.min(48,Math.max(0,s.count));b++) {
        const k=p.offset+b, a=r.fan?(-.5*Math.min(12,60/Math.max(1,r.count-1))*(r.count-1)+k*Math.min(12,60/Math.max(1,r.count-1)))*p.direction*Math.PI/180:0;
        const shift=k*18*p.direction, axis=r.spread_angle*Math.PI/180;
        const x=r.fan?r.x+r.w/2+120*Math.sin(a):r.x+75+shift*Math.cos(axis);
        const y=(r.fan?r.y+r.h-30-120*Math.cos(a):r.y+103+shift*Math.sin(axis))-(22+r.layer*60+k*10.2)*Math.sin(Math.PI/18);
        const hw=(Math.abs(Math.cos(a))*150+Math.abs(Math.sin(a))*200)/2,hh=(Math.abs(Math.sin(a))*150+Math.abs(Math.cos(a))*200)/2;
        cards.push({x,y,angle:a*180/Math.PI,k}); const box={x:x-hw-11,y:y-hh-11,r:x+hw+11,b:y+hh+11};
        const old=groups.get(p.root); groups.set(p.root,old?{x:Math.min(old.x,box.x),y:Math.min(old.y,box.y),r:Math.max(old.r,box.r),b:Math.max(old.b,box.b)}:box);
      } poses[i]={...p,cards,dx:0,dy:0};
    });
    const placed=[];
    for(const [root,original] of [...groups.entries()].sort((a,b)=>l.stacks[a[0]].y-l.stacks[b[0]].y || l.stacks[a[0]].x-l.stacks[b[0]].x || a[0]-b[0])) {
      const box={...original}; let moved;
      if(packed) do {moved=false;for(const o of placed){const a=l.stacks[root],b=l.stacks[o.root];if(a.spread_angle!==b.spread_angle && a.layer!==b.layer)continue;
        if(box.x<o.box.r+12 && box.r>o.box.x-12 && box.y<o.box.b+12 && box.b>o.box.y-12){const px=o.box.r+12-box.x,py=o.box.b+12-box.y;if(py<px){box.y+=py;box.b+=py;}else{box.x+=px;box.r+=px;}moved=true;}}}while(moved);
      const dx=Math.ceil(box.x-original.x),dy=Math.ceil(box.y-original.y);poses.filter(p=>p.root===root).forEach(p=>{p.dx=dx;p.dy=dy;p.cards.forEach(c=>{c.x+=dx;c.y+=dy;});});placed.push({root,box});
    } return poses;
  }
  function newGame(l) {
    const g={remaining:l.stacks.map(s=>s.count),next:Array(l.n_slots).fill(0),slots:[],buffer:[],result:'playing',moves:[]};
    for(let i=0;i<l.n_slots;i++) enter(l,g,i); return g;
  }
  function enter(l,g,i){const t=l.targets[i+g.next[i]*l.n_slots];g.slots[i]=t?{...t,filled:0}:null;if(t)g.next[i]++;}
  function covered(l,g,i){return l.stacks.some((s,j)=>g.remaining[j]>0 && s.layer>l.stacks[i].layer && s.color!==l.stacks[i].color && overlap(s,l.stacks[i]));}
  function settle(l,g){let changed;do{changed=false;g.slots.forEach((s,i)=>{if(s&&s.filled>=s.capacity){enter(l,g,i);changed=true;}});g.slots.forEach(s=>{if(!s)return;for(let i=0;i<g.buffer.length&&s.filled<s.capacity;){if(g.buffer[i]!==s.color){i++;continue;}g.buffer.splice(i,1);s.filled++;changed=true;}});}while(changed);}
  function tap(l,g,i,probe=false){if(g.result!=='playing'||!g.remaining[i]||covered(l,g,i))return false;let count=g.remaining[i];g.remaining[i]=0;g.moves.push(l.stacks[i].id);
    while(count-->0){const s=g.slots.find(s=>s&&s.color===l.stacks[i].color&&s.filled<s.capacity);if(s)s.filled++;else if(g.buffer.length<l.buffer_capacity)g.buffer.push(l.stacks[i].color);else{g.result='overflow';return true;}settle(l,g);}
    if(g.remaining.every(n=>n===0)&&g.buffer.length===0)g.result='won';
    else if(!probe && !l.stacks.some((_,j)=>{if(!g.remaining[j]||covered(l,g,j))return false;const trial=clone(g);tap(l,trial,j,true);return trial.result!=='overflow';}))g.result='stuck';
    return true;
  }
  const api={clone,overlap,normalize,validate,resolve,geometry,newGame,covered,tap};
  if(typeof module!=='undefined')module.exports=api;else root.CardSlotCore=api;
})(globalThis);
