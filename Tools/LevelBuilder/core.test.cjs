const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),C=require('./core.js');
const context={};vm.createContext(context);vm.runInContext(fs.readFileSync(__dirname+'/presets.js','utf8')+';this.levels=CARDSLOT_PRESETS;',context);
let checks=0;
for(const raw of context.levels){
 const l=C.normalize(JSON.parse(JSON.stringify(raw)));assert.deepEqual(C.validate(l),[]);checks++;
 const before=JSON.stringify(l),geometry=C.geometry(l);assert.equal(geometry.length,l.stacks.length);assert.equal(JSON.stringify(l),before);geometry.forEach(p=>p.cards.forEach(c=>assert.ok(Number.isFinite(c.x)&&Number.isFinite(c.y))));checks++;
 const g=C.newGame(l);for(const id of l.solution){const i=l.stacks.findIndex(s=>s.id===id);assert.ok(C.tap(l,g,i),`${l.id}: ${id} blocked`);}assert.equal(g.result,'won',l.id);checks++;
 const restored=C.normalize(JSON.parse(JSON.stringify(l)));assert.equal(JSON.stringify(restored),JSON.stringify(l));checks++;
}
const base=C.normalize(JSON.parse(JSON.stringify(context.levels[0])));
base.stacks=[{...base.stacks[0],id:'a',count:6,fan:true,spread_direction:-1},{...base.stacks[0],id:'b',layer:1,count:3,color:'color_1',on_stack:'a'}];
const p=C.resolve(base);assert.equal(p[1].root,0);assert.equal(p[1].support,0);assert.equal(p[1].offset,6);assert.equal(p[1].direction,-1);checks++;
const bad=C.clone(base);bad.stacks[1].spread_angle=90;assert.ok(C.validate(bad).some(e=>e.includes('cọc đỡ')));checks++;
const balance=C.normalize(JSON.parse(JSON.stringify(context.levels[0])));balance.targets[0].capacity--;assert.ok(C.validate(balance).some(e=>e.includes('sức chứa')));checks++;
const invalid=C.clone(balance);invalid.stacks[1].id=invalid.stacks[0].id;assert.ok(C.validate(invalid).some(e=>e.includes('trùng')));checks++;
console.log(`${checks} checks passed: five shipped solutions, geometry, round trips, inherited reverse fan, invalid support, balance, duplicate IDs.`);
