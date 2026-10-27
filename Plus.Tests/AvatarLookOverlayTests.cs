using System.Data;
using Plus.Core.FigureData;
using Plus.Core.Settings;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Quests;
using Plus.HabboHotel.Rooms.Chat.Filter;
using Plus.HabboHotel.Rooms.Chat.Styles;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Users.Authentication;
using Plus.HabboHotel.Users.Clothing;
using Xunit;

namespace Plus.Tests;

public class AvatarLookOverlayTests
{
    [Fact]
    public void UpdateFigureSavesTheLookWithoutTheOverlayAndKeepsItLive()
    {
        var database = new RecordingDatabase();
        var figures = new EchoFigures();
        var habbo = new Habbo { Id = 7, Look = "hd-180-1", Gender = "M", Clothing = new ClothingComponent() };
        var (client, _) = HabbiconTestSupport.Client(habbo);
        var service = Profiles(database, figures, new ArmorOverlay());

        service.UpdateFigure(client, new FigureUpdateRequest("M", "hd-180-1.ch-3001-1.ca-9999-1"));

        Assert.Equal("hd-180-1.ch-3001-1", (string)Assert.Single(figures.Calls)[0]);
        var written = SavedLook(database);
        Assert.Equal("hd-180-1.ch-3001-1", written);
        Assert.Equal("hd-180-1.ch-3001-1.ca-9999-1", habbo.Look);
    }

    [Fact]
    public void UpdateFigureWithoutOverlaysSavesTheRequestedLookUnchanged()
    {
        var database = new RecordingDatabase();
        var figures = new EchoFigures();
        var habbo = new Habbo { Id = 7, Look = "hd-180-1", Gender = "M", Clothing = new ClothingComponent() };
        var (client, _) = HabbiconTestSupport.Client(habbo);
        var service = Profiles(database, figures);

        service.UpdateFigure(client, new FigureUpdateRequest("M", "hd-180-1.ch-3001-1.ca-9999-1"));

        Assert.Equal("hd-180-1.ch-3001-1.ca-9999-1", (string)Assert.Single(figures.Calls)[0]);
        Assert.Equal("hd-180-1.ch-3001-1.ca-9999-1", SavedLook(database));
        Assert.Equal("hd-180-1.ch-3001-1.ca-9999-1", habbo.Look);
    }

    [Fact]
    public void ApplyMannequinSavesTheLookWithoutTheOverlayAndKeepsItLive()
    {
        var database = new RecordingDatabase();
        var figures = new EchoFigures();
        var habbo = new Habbo { Id = 7, Look = "hd-180-1", Gender = "M", Clothing = new ClothingComponent() };
        var (client, _) = HabbiconTestSupport.Client(habbo);
        var service = Profiles(database, figures, new ArmorOverlay());

        service.ApplyMannequin(client, new FigureUpdateRequest("M", "hd-180-1.ch-3001-1.ca-9999-1"));

        Assert.Equal("hd-180-1.ch-3001-1", (string)Assert.Single(figures.Calls)[0]);
        Assert.Equal("hd-180-1.ch-3001-1", SavedLook(database));
        Assert.Equal("hd-180-1.ch-3001-1.ca-9999-1", habbo.Look);
    }

    private static UserProfileService Profiles(RecordingDatabase database, EchoFigures figures, params IAvatarLookOverlay[] overlays) =>
        new(figures, Untouched<IAchievementManager>(), Untouched<IQuestManager>(), Untouched<IWordFilterManager>(), database,
            TimeProvider.System, Untouched<IChatStyleManager>(), Untouched<IRewardTrackManager>(),
            new AccountSessionGate(), Untouched<ISettingsManager>(), overlays.Length == 0 ? null : overlays);

    // The only string parameter holding a figure is the look the UPDATE writes to users.look.
    private static string? SavedLook(RecordingDatabase database)
    {
        var write = Assert.Single(database.Writes, entry => entry.Sql.StartsWith("UPDATE users SET look", StringComparison.OrdinalIgnoreCase));

        return Assert.Single(write.Parameters.Values.OfType<string>().Where(value => value.Contains("hd-", StringComparison.Ordinal)));
    }

    private static T Untouched<T>() where T : class => CatalogSnapshotTestSupport.Proxy<T>((method, _) =>
        method == nameof(IAchievementManager.ProgressAchievement) ? (object)false : null);

    private sealed class ArmorOverlay : IAvatarLookOverlay
    {
        private const string Part = ".ca-9999-1";
        public string Strip(Habbo habbo, string figure) => figure.Replace(Part, string.Empty, StringComparison.Ordinal);
        public string Apply(Habbo habbo, string look) => look + Part;
    }

    private sealed class EchoFigures : IFigureDataManager
    {
        public List<object[]> Calls { get; } = new();
        public void Init() { }
        public string ProcessFigure(string figure, string gender, ICollection<Plus.HabboHotel.Users.Clothing.Parts.ClothingParts>? clothingParts, int clubLevel)
        {
            Calls.Add(new object[] { figure, gender, clubLevel });

            return figure;
        }
        public Plus.Core.FigureData.Types.Palette? GetPalette(int colorId) => null;
        public bool TryGetPalette(int palletId, out Plus.Core.FigureData.Types.Palette? palette)
        {
            palette = null;

            return false;
        }
        public int GetRandomColor(int palletId, int clubLevel = 0) => 0;
        public string FilterFigure(string figure) => figure;
    }

    private sealed class RecordingDatabase : Plus.Database.IDatabase
    {
        public List<(string Sql, Dictionary<string, object?> Parameters)> Writes { get; } = new();
        public bool IsConnected() => true;
        public IDbConnection Connection() => new RecordingConnection(this);
    }

    private sealed class RecordingConnection(RecordingDatabase database) : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 1;
        public string Database => "";
        public ConnectionState State { get; private set; } = ConnectionState.Open;
        public IDbTransaction BeginTransaction() => throw new NotSupportedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotSupportedException();
        public void ChangeDatabase(string databaseName) { }
        public void Close() => State = ConnectionState.Closed;
        public IDbCommand CreateCommand() => new RecordingCommand(database);
        public void Open() => State = ConnectionState.Open;
        public void Dispose() { }
    }

    private sealed class RecordingCommand(RecordingDatabase database) : IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDbConnection? Connection { get; set; }
        public IDataParameterCollection Parameters { get; } = new RecordingParameters();
        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }
        public void Cancel() { }
        public IDbDataParameter CreateParameter() => new RecordingParameter();
        public void Dispose() { }
        public int ExecuteNonQuery()
        {
            database.Writes.Add((CommandText, Parameters.Cast<RecordingParameter>().ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value)));

            return 1;
        }
        public IDataReader ExecuteReader() => throw new NotSupportedException();
        public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotSupportedException();
        public object? ExecuteScalar() => throw new NotSupportedException();
        public void Prepare() { }
    }

    private sealed class RecordingParameters : List<RecordingParameter>, IDataParameterCollection
    {
        public object? this[string parameterName]
        {
            get => Find(parameter => parameter.ParameterName == parameterName);
            set => throw new NotSupportedException();
        }
        public bool Contains(string parameterName) => this.Any(parameter => parameter.ParameterName == parameterName);
        public int IndexOf(string parameterName) => FindIndex(parameter => parameter.ParameterName == parameterName);
        public void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
    }

    private sealed class RecordingParameter : IDbDataParameter
    {
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = "";
        public string SourceColumn { get; set; } = "";
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
    }
}
