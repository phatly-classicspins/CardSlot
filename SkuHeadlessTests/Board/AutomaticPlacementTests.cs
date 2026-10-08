using System.Collections.Generic;
using System.Linq;
using ClassicSpins.PrototypeFramework.Domain;
using Game.Domain;
using NUnit.Framework;
namespace CardSlot.SkuHeadlessTests.Board
{
    public class AutomaticPlacementTests
    {
        [Test]
        public void Crossing_axis_starts_a_bridge_and_same_axis_cards_continue_it()
        {
            var level=new LevelData();
            level.Stacks.AddRange(new[] {
                new StackSpec("horizontal",0,0,350,206,0,0,6),
                new StackSpec("vertical",100,0,350,500,2,1,6) {SpreadAngle=90},
                new StackSpec("verticalTop",100,1,350,500,3,2,6) {SpreadAngle=90}});
            var p=StackPlacementRules.Resolve(level);
            Assert.That((p[1].Root,p[1].Support,p[1].Offset),Is.EqualTo((1,-1,0)));
            Assert.That((p[2].Root,p[2].Support,p[2].Offset),Is.EqualTo((1,1,6)));
        }

        [Test]
        public void Root_direction_is_automatic_or_explicit_and_inherited_across_layers()
        {
            var level = new LevelData();
            level.Stacks.AddRange(new[] {
                new StackSpec("left",0,0,100,100,0,0,6),
                new StackSpec("right",300,0,100,100,0,1,6),
                new StackSpec("leftTop",10,10,100,100,1,2,6) {SpreadDirection=1},
                new StackSpec("rightTop",310,10,100,100,1,3,6) {SpreadDirection=-1}});
            var p=StackPlacementRules.Resolve(level);
            Assert.That(p.Select(x=>x.Direction), Is.EqualTo(new[]{-1,1,-1,1}));
            level.Stacks[0].SpreadDirection=1; level.Stacks[1].SpreadDirection=-1;
            p=StackPlacementRules.Resolve(level);
            Assert.That(p.Select(x=>x.Direction), Is.EqualTo(new[]{1,-1,1,-1}));
        }

        [Test]
        public void Automatic_chain_inherits_root_and_reserves_authored_counts_without_hints()
        {
            var level = new LevelData();
            level.Stacks.AddRange(new[] {
                new StackSpec("top",20,20,100,100,2,2,5),
                new StackSpec("base",0,0,100,100,0,0,6) {Fan=true, Spread=true},
                new StackSpec("middle",10,10,100,100,1,1,3)});
            var p = StackPlacementRules.Resolve(level);
            Assert.That((p[2].Root,p[2].Support,p[2].Offset), Is.EqualTo((1,1,6)));
            Assert.That((p[0].Root,p[0].Support,p[0].Offset), Is.EqualTo((1,2,9)));
            Assert.That(level.Stacks.All(s=>s.OnStack==null), Is.True);
            Assert.That(level.Stacks[0].X, Is.EqualTo(20), "layout does not rewrite covering rectangles");
        }

        [Test]
        public void Support_prefers_highest_layer_then_overlap_and_is_independent_of_input_order()
        {
            var level = new LevelData();
            level.Stacks.AddRange(new[] {
                new StackSpec("left",0,0,100,100,0,0,6),
                new StackSpec("right",100,0,100,100,0,1,6),
                new StackSpec("middle",80,0,100,100,1,2,3),
                new StackSpec("top",0,0,100,100,2,3,4)});
            var p = StackPlacementRules.Resolve(level);
            Assert.That(level.Stacks[p[2].Support].Id, Is.EqualTo("right"), "larger overlap wins within a layer");
            Assert.That(level.Stacks[p[3].Support].Id, Is.EqualTo("middle"), "highest overlapping layer wins before area");
            var before=level.Stacks.Select((s,i)=>(s.Id,Root:level.Stacks[p[i].Root].Id,p[i].Offset)).OrderBy(x=>x.Id).ToArray();
            level.Stacks.Reverse(); p=StackPlacementRules.Resolve(level);
            var after=level.Stacks.Select((s,i)=>(s.Id,Root:level.Stacks[p[i].Root].Id,p[i].Offset)).OrderBy(x=>x.Id).ToArray();
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void Siblings_reserve_distinct_card_intervals_and_disjoint_stacks_remain_roots()
        {
            var level=new LevelData();
            level.Stacks.AddRange(new[] {
                new StackSpec("root",0,0,300,100,0,0,6),
                new StackSpec("a",0,0,100,100,1,1,3),
                new StackSpec("b",200,0,100,100,1,2,5),
                new StackSpec("separate",500,0,100,100,1,3,2)});
            var p=StackPlacementRules.Resolve(level);
            Assert.That((p[1].Offset,p[2].Offset), Is.EqualTo((6,9)));
            Assert.That((p[3].Root,p[3].Support,p[3].Offset), Is.EqualTo((3,-1,0)));
        }

        [Test]
        public void Generated_and_expanded_levels_get_automatic_placements()
        {
            int checkedLevels=0, linked=0;
            for(long seed=1;seed<=40;seed++) {
                var groups=LevelGenerator.Generate("auto",new GenParams {Colors=4,TargetsPerColor=2,MaxLayer=2,BufferCapacity=6},new Pcg32(seed));
                if(groups==null) continue;
                groups.Stacks[0].Fan=true; groups.Stacks[0].Spread=true; groups.Stacks[0].SpreadDirection=-1; groups.Stacks[0].SpreadAngle=90;
                var cards=LevelGenerator.ExpandToCards(groups,6,38);
                Assert.That(cards.Stacks[0].Fan && cards.Stacks[0].Spread, Is.True);
                Assert.That(cards.Stacks[0].SpreadDirection, Is.EqualTo(-1));
                Assert.That(cards.Stacks[0].SpreadAngle, Is.EqualTo(90));
                Assert.That(cards.Stacks.All(s=>s.OnStack==null), Is.True);
                var p=StackPlacementRules.Resolve(cards);
                for(int i=0;i<p.Length;i++) {
                    if(p[i].Support<0) continue;
                    linked++;
                    Assert.That(cards.Stacks[p[i].Support].Layer, Is.LessThan(cards.Stacks[i].Layer));
                    Assert.That(p[i].Offset, Is.GreaterThanOrEqualTo(cards.Stacks[p[i].Root].Count));
                }
                checkedLevels++;
            }
            Assert.That(checkedLevels, Is.GreaterThan(0)); Assert.That(linked, Is.GreaterThan(0));
        }
    }
}
