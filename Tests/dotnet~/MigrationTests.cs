using Scellecs.Morpeh;
using Xunit.Abstractions;

namespace Tests;

[Collection("Sequential")]
public class MigrationTests {
    private readonly World world;
    
    public MigrationTests(ITestOutputHelper output) {
        world = World.Create();
        MLogger.SetInstance(new XUnitLogger(output));
    }
    
    [Fact]
    public void ArchetypeChanges() {
        var entity = this.world.CreateEntity();
        var previousArchetype = default(ArchetypeHash);
        
        this.world.GetStash<Test1>().Set(entity);
        
        // We haven't committed the changes yet, so the archetype should be the same
        Assert.Equal(previousArchetype, world.ArchetypeOf(entity));
        
        this.world.Commit();
        
        // Now that we've committed the changes, the archetype should have changed
        Assert.NotEqual(previousArchetype, world.ArchetypeOf(entity));
        var archetype = this.world.GetArchetype(default(ArchetypeHash).With<Test1>());
        Assert.Equal(1, archetype.length);
        
        this.world.GetStash<Test1>().Remove(entity);
        this.world.Commit();
        
        Assert.Equal(0, archetype.length);
        
        // The archetype should have changed again back to the original
        Assert.Equal(previousArchetype, world.ArchetypeOf(entity));
    }
    
    [Fact]
    public void SingleComponentMigrates() {
        var entity = this.world.CreateEntity();
        var baseArchetype = default(ArchetypeHash);
        
        Assert.Equal(baseArchetype, world.ArchetypeOf(entity));
        
        this.world.GetStash<Test1>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>(), world.ArchetypeOf(entity));
        
        var archetypeT1 = this.world.GetArchetype(baseArchetype.With<Test1>());
        Assert.Equal(1, archetypeT1.length);
        
        this.world.GetStash<Test2>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        Assert.Equal(0, archetypeT1.length);
        
        var archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        
        this.world.GetStash<Test3>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>().With<Test3>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2T3 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>().With<Test3>());
        Assert.Equal(1, archetypeT1T2T3.length);
        Assert.Equal(0, archetypeT1T2.length);
        
        this.world.GetStash<Test4>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>().With<Test3>().With<Test4>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2T3T4 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>().With<Test3>().With<Test4>());
        Assert.Equal(1, archetypeT1T2T3T4.length);
        Assert.Equal(0, archetypeT1T2T3.length);
        
        this.world.GetStash<Test4>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>().With<Test3>(), world.ArchetypeOf(entity));
        
        archetypeT1T2T3 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>().With<Test3>());
        Assert.Equal(1, archetypeT1T2T3.length);
        Assert.Equal(0, archetypeT1T2T3T4.length);
        
        this.world.GetStash<Test3>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        Assert.Equal(0, archetypeT1T2T3.length);
        
        this.world.GetStash<Test2>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>(), world.ArchetypeOf(entity));
        
        archetypeT1 = this.world.GetArchetype(baseArchetype.With<Test1>());
        Assert.Equal(1, archetypeT1.length);
        Assert.Equal(0, archetypeT1T2.length);
        
        this.world.GetStash<Test1>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype, world.ArchetypeOf(entity));
        
        Assert.Equal(0, archetypeT1.length);
    }
    
    [Fact]
    public void MultipleComponentsMigrate() {
        var entity = this.world.CreateEntity();
        var baseArchetype = world.ArchetypeOf(entity);
        
        this.world.GetStash<Test1>().Set(entity);
        this.world.GetStash<Test2>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        
        this.world.GetStash<Test3>().Set(entity);
        this.world.GetStash<Test4>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>().With<Test3>().With<Test4>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2T3T4 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>().With<Test3>().With<Test4>());
        Assert.Equal(1, archetypeT1T2T3T4.length);
        Assert.Equal(0, archetypeT1T2.length);
        
        this.world.GetStash<Test4>().Remove(entity);
        this.world.GetStash<Test3>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        Assert.Equal(0, archetypeT1T2T3T4.length);
        
        this.world.GetStash<Test2>().Remove(entity);
        this.world.GetStash<Test1>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype, world.ArchetypeOf(entity));
        
        Assert.Equal(0, archetypeT1T2.length);
    }
    
    [Fact]
    public void RemoveSameComponentDoesntMigrate() {
        var entity = this.world.CreateEntity();
        var baseArchetype = world.ArchetypeOf(entity);
        
        this.world.GetStash<Test1>().Set(entity);
        this.world.GetStash<Test2>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        
        this.world.GetStash<Test3>().Set(entity);
        this.world.GetStash<Test3>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        Assert.Equal(1, archetypeT1T2.length);
    }
    
    [Fact]
    public void NoStructuralChangeDoesntMigrate() {
        var entity = this.world.CreateEntity();
        var baseArchetype = world.ArchetypeOf(entity);
        
        this.world.GetStash<Test1>().Set(entity);
        this.world.GetStash<Test2>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2 = this.world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        
        this.world.GetStash<Test3>().Remove(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        Assert.Equal(1, archetypeT1T2.length);
    }
    
    [Fact]
    public void DisposeMigrates() {
        var entity = world.CreateEntity();
        var baseArchetype = default(ArchetypeHash);
        
        this.world.GetStash<Test1>().Set(entity);
        this.world.GetStash<Test2>().Set(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype.With<Test1>().With<Test2>(), world.ArchetypeOf(entity));
        
        var archetypeT1T2 = world.GetArchetype(baseArchetype.With<Test1>().With<Test2>());
        Assert.Equal(1, archetypeT1T2.length);
        
        this.world.RemoveEntity(entity);
        this.world.Commit();
        Assert.Equal(baseArchetype, world.ArchetypeOf(entity));
        
        Assert.Equal(0, archetypeT1T2.length);
    }
}