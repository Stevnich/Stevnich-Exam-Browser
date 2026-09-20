using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Logging.Contracts;

namespace SafeExamBrowser.Service.Operations
{
	internal class SessionInitializationOperation : SessionOperation
	{
		private readonly ILogger logger;
		private readonly Func<string, EventWaitHandle> serviceEventFactory;

		public SessionInitializationOperation(ILogger logger, Func<string, EventWaitHandle> serviceEventFactory, SessionContext sessionContext) : base(sessionContext)
		{
			this.logger = logger;
			this.serviceEventFactory = serviceEventFactory;
		}

		public override OperationResult Perform()
		{
			logger.Info("Initializing new session...");
			logger.Info($" -> Client-ID: {Context.Configuration.AppConfig.ClientId}");
			logger.Info($" -> Runtime-ID: {Context.Configuration.AppConfig.RuntimeId}");
			logger.Info($" -> Session-ID: {Context.Configuration.SessionId}");
			logger.Info($" -> ServiceEventName: {Context.Configuration.AppConfig.ServiceEventName}");
			logger.Info($" -> AutoRestoreMechanism: {(Context.AutoRestoreMechanism != null ? "Available" : "Not available")}");

			// Detailed time logging and optional NTP sync
			try
			{
				// Get detailed local system time info
				var systemInfo = AccurateTimeUtils.FromSystemClock();
				logger.Info($" -> Current Session is started in: {systemInfo.LocalTime:yyyy-MM-dd HH:mm:ss.fff} ({systemInfo.TimeZoneId} {systemInfo.UtcOffset:+hh\\:mm})");
				logger.Info($"    UTC: {systemInfo.UtcTime:yyyy-MM-dd HH:mm:ss.fff}");
				logger.Info($"    Unix(ms): {systemInfo.UnixMilliseconds}, WeekOfYear: {systemInfo.WeekOfYear}, DayOfYear: {systemInfo.DayOfYear}");
				logger.Info($"    RFC3339: {systemInfo.Rfc3339}");
				logger.Info($"    ISOWeekDate: {systemInfo.IsoWeekDate}");

				// Attempt quick NTP sync (blocking with short timeout to keep Perform deterministic)
				var ntpTask = AccurateTimeUtils.GetAccurateTimeAsync("pool.ntp.org", 2000); // timeout 2s
				if (ntpTask.Wait(2500)) // Wait a bit longer than internal timeout
				{
					var (ntpInfo, offset) = ntpTask.Result;
					if (offset.HasValue)
					{
						logger.Info($"    NTP-synchronized UTC: {ntpInfo.UtcTime:yyyy-MM-dd HH:mm:ss.fff}");
						logger.Info($"    NTP offset (ntp - system): {offset.Value.TotalMilliseconds} ms");
						logger.Info($"    Corrected Local: {ntpInfo.LocalTime:yyyy-MM-dd HH:mm:ss.fff} ({ntpInfo.TimeZoneId})");
					}
					else
					{
						logger.Info("    NTP sync unavailable, using system clock.");
					}
				}
				else
				{
					logger.Info("    NTP sync timed out, using system clock.");
				}
			}
			catch (Exception ex)
			{
				logger.Error($"    Time synchronization failed: {ex.Message}");
			}

			logger.Info("Stopping auto-restore mechanism...");
			Context.AutoRestoreMechanism.Stop();

			InitializeServiceEvent();

			return OperationResult.Success;
		}

		public override OperationResult Revert()
		{
			var success = true;
			var wasRunning = Context.IsRunning;

			logger.Info("Starting auto-restore mechanism...");
			Context.AutoRestoreMechanism.Start();

			logger.Info("Clearing session data...");
			Context.Configuration = null;
			Context.IsRunning = false;

			if (Context.ServiceEvent != null && wasRunning)
			{
				success = Context.ServiceEvent.Set();

				if (success)
				{
					logger.Info("Successfully informed runtime about session termination.");
				}
				else
				{
					logger.Error("Failed to inform runtime about session termination!");
				}
			}

			return success ? OperationResult.Success : OperationResult.Failed;
		}

		private void InitializeServiceEvent()
		{
			if (Context.ServiceEvent != null)
			{
				logger.Info("Closing service event from previous session...");
				Context.ServiceEvent.Close();
				logger.Info("Service event successfully closed.");
			}

			logger.Info("Attempting to create new service event...");
			Context.ServiceEvent = serviceEventFactory.Invoke(Context.Configuration.AppConfig.ServiceEventName);
			logger.Info("Service event successfully created.");
		}
	}

	// Internal helper providing detailed time info and NTP sync
	internal static class AccurateTimeUtils
	{
      public class AccurateTimeInfo
		{
			public DateTime LocalTime { get; set; }
			public DateTime UtcTime { get; set; }
			public TimeSpan UtcOffset { get; set; }
			public string TimeZoneId { get; set; }
			public bool IsDaylightSaving { get; set; }
			public int Year => LocalTime.Year;
			public int Month => LocalTime.Month;
			public int Day => LocalTime.Day;
			public int Hour => LocalTime.Hour;
			public int Minute => LocalTime.Minute;
			public int Second => LocalTime.Second;
			public int Millisecond => LocalTime.Millisecond;
			public long UnixMilliseconds => new DateTimeOffset(UtcTime).ToUnixTimeMilliseconds();
			public long UnixSeconds => new DateTimeOffset(UtcTime).ToUnixTimeSeconds();
			public int DayOfYear => LocalTime.DayOfYear;
			public int WeekOfYear { get; set; }
			public string IsoWeekDate { get; set; }
			public string Rfc3339 { get; set; }
		}

        // ISO-8601 week number
        public static int GetIso8601WeekOfYear(DateTime time)
		{
			var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
			if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
			{
				time = time.AddDays(3);
			}
			return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time,
				System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
		}

		public static string GetIsoWeekDate(DateTime dt)
		{
			var week = GetIso8601WeekOfYear(dt);
			var year = dt.Year;
			// Adjust year for ISO week that belongs to previous/next year
			if (dt.Month == 1 && week >= 52) year = dt.Year - 1;
			if (dt.Month == 12 && week == 1) year = dt.Year + 1;
			return $"{year}-W{week:00}-{(int) dt.DayOfWeek}";
		}

		public static AccurateTimeInfo FromSystemClock()
		{
			var local = DateTime.Now;
			var utc = DateTime.UtcNow;
			var tz = TimeZoneInfo.Local;
			var offset = tz.GetUtcOffset(local);
			var week = GetIso8601WeekOfYear(local);
			var isoWeekDate = GetIsoWeekDate(local);
			var rfc3339 = local.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
			return new AccurateTimeInfo
			{
				LocalTime = local,
				UtcTime = utc,
				UtcOffset = offset,
				TimeZoneId = tz.Id,
				IsDaylightSaving = tz.IsDaylightSavingTime(local),
				WeekOfYear = week,
				IsoWeekDate = isoWeekDate,
				Rfc3339 = rfc3339
			};
		}

		// Simple NTP query. timeoutMs applies to socket receive timeout.
		public static async Task<DateTime?> GetNetworkTimeAsync(string ntpServer = "pool.ntp.org", int timeoutMs = 2000)
		{
			try
			{
				var ntpData = new byte[48];
				ntpData[0] = 0x1B;

				var addresses = await Dns.GetHostAddressesAsync(ntpServer).ConfigureAwait(false);
				if (addresses == null || addresses.Length == 0) return null;
				var ipEndPoint = new IPEndPoint(addresses[0], 123);

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					socket.ReceiveTimeout = timeoutMs;
					await socket.ConnectAsync(ipEndPoint).ConfigureAwait(false);
					await socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None).ConfigureAwait(false);

					var received = await socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None).ConfigureAwait(false);
					if (received < 48) return null;

					// rest of processing continues inside using block
					const byte offsetTransmitTime = 40;
					ulong intPart = (ulong) ntpData[offsetTransmitTime + 0] << 24 |
									(ulong) ntpData[offsetTransmitTime + 1] << 16 |
									(ulong) ntpData[offsetTransmitTime + 2] << 8 |
									(ulong) ntpData[offsetTransmitTime + 3];

					ulong fractPart = (ulong) ntpData[offsetTransmitTime + 4] << 24 |
									  (ulong) ntpData[offsetTransmitTime + 5] << 16 |
									  (ulong) ntpData[offsetTransmitTime + 6] << 8 |
									  (ulong) ntpData[offsetTransmitTime + 7];

					var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000UL);
					var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long) milliseconds);
					return DateTime.SpecifyKind(networkDateTime, DateTimeKind.Utc);
				}
			}
			catch
			{
				return null;
			}
		}

		// Returns corrected AccurateTimeInfo and offset (ntpUtc - systemUtc) if available
		public static async Task<(AccurateTimeInfo info, TimeSpan? ntpOffset)> GetAccurateTimeAsync(string ntpServer = "pool.ntp.org", int timeoutMs = 2000)
		{
			var system = FromSystemClock();
			var ntpUtc = await GetNetworkTimeAsync(ntpServer, timeoutMs).ConfigureAwait(false);
			if (ntpUtc.HasValue)
			{
				var systemUtc = DateTime.UtcNow;
				var offset = ntpUtc.Value - systemUtc;
				var correctedLocal = system.LocalTime.Add(offset);
				var tz = TimeZoneInfo.Local;
				var correctedInfo = new AccurateTimeInfo
				{
					LocalTime = correctedLocal,
					UtcTime = ntpUtc.Value,
					UtcOffset = tz.GetUtcOffset(correctedLocal),
					TimeZoneId = tz.Id,
					IsDaylightSaving = tz.IsDaylightSavingTime(correctedLocal),
					WeekOfYear = GetIso8601WeekOfYear(correctedLocal),
					IsoWeekDate = GetIsoWeekDate(correctedLocal),
					Rfc3339 = correctedLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK")
				};
				return (correctedInfo, offset);
			}
			else
			{
				return (system, null);
			}
		}
	}
}