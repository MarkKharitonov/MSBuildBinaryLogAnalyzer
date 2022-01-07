using Moq;
using MSBuildBinaryLogAnalyzer.TraceEvents;
using NUnit.Framework;

namespace MSBuildBinaryLogAnalyzer.Tests
{
    public class MSBuildNodeTraceEventCompressorTests
    {
        private static Dictionary<string, (string[] Input, TraceEvent[] Expected)> s_testCaseData = new()
        {
            ["simple1"] = (new[]
            {
               "XXX",
            }, new[]
            {
                new TraceEvent { name = "X", ts = 0, dur = 3 },
            }),
            ["simple2"] = (new[]
            {
               "XXX   ",
               "   YYY",
            }, new[]
            {
                new TraceEvent { name = "X", ts = 0, dur = 3 },
                new TraceEvent { name = "Y", ts = 3, dur = 3 },
            }),
            ["simple2_with_gaps"] = (new[]
            {
               " XXX    ",
               "     YYY",
            }, new[]
            {
                new TraceEvent { name = "X", ts = 1, dur = 3 },
                new TraceEvent { name = "Y", ts = 5, dur = 3 },
            }),
            ["overlap2"] = (new[]
            {
               "XXX  ",
               "  YYY",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 2, args = new(){ ["2.duration"] = "3s", ["1.start"] = "0s", ["3.end"] = "3s", ["0.percents"] = "66.67%" } },
                new TraceEvent { name = "Y", ts = 2, dur = 3 },
                new TraceEvent { name = "|X", ts = 3, dur = 0, args = new(){ ["2.duration"] = "3s", ["1.start"] = "0s", ["3.end"] = "3s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap3"] = (new[]
            {
               "XXX  ",
               "  YYY",
               "    ZZZ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 2, args = new(){ ["2.duration"] = "3s", ["1.start"] = "0s", ["3.end"] = "3s", ["0.percents"] = "66.67%" } },
                new TraceEvent { name = "|Y", ts = 2, dur = 2, args = new(){ ["2.duration"] = "3s", ["1.start"] = "2s", ["3.end"] = "5s", ["0.percents"] = "66.67%" } },
                new TraceEvent { name = "Z", ts = 4, dur = 3 },
                new TraceEvent { name = "|X", ts = 3, dur = 0, args = new(){ ["2.duration"] = "3s", ["1.start"] = "0s", ["3.end"] = "3s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|Y", ts = 5, dur = 0, args = new(){ ["2.duration"] = "3s", ["1.start"] = "2s", ["3.end"] = "5s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap3_long"] = (new[]
            {
               "XXXXXX ",
               "  YYY  ",
               "    ZZZ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 2, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "33.33%" } },
                new TraceEvent { name = "|Y", ts = 2, dur = 2, args = new(){ ["2.duration"] = "3s", ["1.start"] = "2s", ["3.end"] = "5s", ["0.percents"] = "66.67%" } },
                new TraceEvent { name = "Z", ts = 4, dur = 3 },
                new TraceEvent { name = "|Y", ts = 5, dur = 0, args = new(){ ["2.duration"] = "3s", ["1.start"] = "2s", ["3.end"] = "5s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|X", ts = 6, dur = 0, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap3_long_with_gap"] = (new[]
            {
               "XXXXXX    ",
               "  YYY     ",
               "       ZZZ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 2, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "33.33%" } },
                new TraceEvent { name = "Y", ts = 2, dur = 3 },
                new TraceEvent { name = "|X", ts = 5, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "Z", ts = 7, dur = 3 },
            }),
            ["overlap_pyramid"] = (new[]
            {
               "XXXXXXX",
               " YYYYY ",
               "  ZZZ  ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 1, args = new(){ ["2.duration"] = "7s", ["1.start"] = "0s", ["3.end"] = "7s", ["0.percents"] = "14.29%" } },
                new TraceEvent { name = "|Y", ts = 1, dur = 1, args = new(){ ["2.duration"] = "5s", ["1.start"] = "1s", ["3.end"] = "6s", ["0.percents"] = "20.00%" } },
                new TraceEvent { name = "Z", ts = 2, dur = 3 },
                new TraceEvent { name = "|Y", ts = 5, dur = 1, args = new(){ ["2.duration"] = "5s", ["1.start"] = "1s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|X", ts = 6, dur = 1, args = new(){ ["2.duration"] = "7s", ["1.start"] = "0s", ["3.end"] = "7s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap_parallelogram"] = (new[]
            {
               "XXXXXX ",
               " YYYYYY",
               "  ZZZ   ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "16.67%" } },
                new TraceEvent { name = "|Y", ts = 1, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "1s", ["3.end"] = "7s", ["0.percents"] = "16.67%" } },
                new TraceEvent { name = "Z", ts = 2, dur = 3 },
                new TraceEvent { name = "|X", ts = 5, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|Y", ts = 6, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "1s", ["3.end"] = "7s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap_same_end"] = (new[]
            {
               "XXXXXX",
               " YYYYY",
               "  ZZZ ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "16.67%" } },
                new TraceEvent { name = "|Y", ts = 1, dur = 1, args = new(){ ["2.duration"] = "5s", ["1.start"] = "1s", ["3.end"] = "6s", ["0.percents"] = "20.00%" } },
                new TraceEvent { name = "Z", ts = 2, dur = 3 },
                new TraceEvent { name = "|Y", ts = 5, dur = 1, args = new(){ ["2.duration"] = "5s", ["1.start"] = "1s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|X", ts = 6, dur = 0, args = new(){ ["2.duration"] = "6s", ["1.start"] = "0s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
            }),
            ["overlap4"] = (new[]
            {
               "XXXX    YYYY",
               "  AAAA      ",
               "    BBBBB   ",
            }, new[]
            {
                new TraceEvent { name = "|X", ts = 0, dur = 2, args = new(){ ["2.duration"] = "4s", ["1.start"] = "0s", ["3.end"] = "4s", ["0.percents"] = "50.00%" } },
                new TraceEvent { name = "|A", ts = 2, dur = 2, args = new(){ ["2.duration"] = "4s", ["1.start"] = "2s", ["3.end"] = "6s", ["0.percents"] = "50.00%" } },
                new TraceEvent { name = "|B", ts = 4, dur = 4, args = new(){ ["2.duration"] = "5s", ["1.start"] = "4s", ["3.end"] = "9s", ["0.percents"] = "80.00%" } },
                new TraceEvent { name = "Y", ts = 8, dur = 4 },
                new TraceEvent { name = "|X", ts = 4, dur = 0, args = new(){ ["2.duration"] = "4s", ["1.start"] = "0s", ["3.end"] = "4s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|A", ts = 6, dur = 0, args = new(){ ["2.duration"] = "4s", ["1.start"] = "2s", ["3.end"] = "6s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|B", ts = 9, dur = 0, args = new(){ ["2.duration"] = "5s", ["1.start"] = "4s", ["3.end"] = "9s", ["0.percents"] = "100.00%" } },
            }),
            ["MainNode1"] = (new[]
            {
               "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
               "       BB                                       ",
               "          C                                     ",
               "           DDDDDD                               ",
               "             EEEEEE                             ",
               "              FFFFF                             ",
               "                      GGG                       ",
               "                         HHHHHHHHHHHHH          ",
               "                            III                 ",
               "                               JJJ              ",
            }, new[]
            {
                new TraceEvent { name = "|A", ts = 0, dur = 7, args = new(){ ["2.duration"] = "48s", ["1.start"] = "0s", ["3.end"] = "48s", ["0.percents"] = "14.58%" } },
                new TraceEvent { name = "B", ts = 7, dur = 2 },
                new TraceEvent { name = "|A", ts = 9, dur = 1, args = new(){ ["2.duration"] = "48s", ["1.start"] = "0s", ["3.end"] = "48s", ["0.percents"] = "20.83%" } },
                new TraceEvent { name = "C", ts = 10, dur = 1 },
                new TraceEvent { name = "|D", ts = 11, dur = 2, args = new(){ ["2.duration"] = "6s", ["1.start"] = "11s", ["3.end"] = "17s", ["0.percents"] = "33.33%" } },
                new TraceEvent { name = "|E", ts = 13, dur = 1, args = new(){ ["2.duration"] = "6s", ["1.start"] = "13s", ["3.end"] = "19s", ["0.percents"] = "16.67%" } },
                new TraceEvent { name = "F", ts = 14, dur = 5 },
                new TraceEvent { name = "|D", ts = 17, dur = 0, args = new(){ ["2.duration"] = "6s", ["1.start"] = "11s", ["3.end"] = "17s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|E", ts = 19, dur = 0, args = new(){ ["2.duration"] = "6s", ["1.start"] = "13s", ["3.end"] = "19s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|A", ts = 19, dur = 3, args = new(){ ["2.duration"] = "48s", ["1.start"] = "0s", ["3.end"] = "48s", ["0.percents"] = "45.83%" } },
                new TraceEvent { name = "G", ts = 22, dur = 3 },
                new TraceEvent { name = "|H", ts = 25, dur = 3, args = new(){ ["2.duration"] = "13s", ["1.start"] = "25s", ["3.end"] = "38s", ["0.percents"] = "23.08%" } },
                new TraceEvent { name = "I", ts = 28, dur = 3 },
                new TraceEvent { name = "J", ts = 31, dur = 3 },
                new TraceEvent { name = "|H", ts = 34, dur = 4, args = new(){ ["2.duration"] = "13s", ["1.start"] = "25s", ["3.end"] = "38s", ["0.percents"] = "100.00%" } },
                new TraceEvent { name = "|A", ts = 38, dur = 10, args = new(){ ["2.duration"] = "48s", ["1.start"] = "0s", ["3.end"] = "48s", ["0.percents"] = "100.00%" } },
            }),
        };
        private static IEnumerable<string> s_testCaseNames = s_testCaseData.Keys;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TraceEvent.MICRO_SECONDS = 1;
        }

        [TestCaseSource(nameof(s_testCaseNames))]
        public void T(string key)
        {
            var input = s_testCaseData[key].Input.SelectMany(StringToTraceEvents).OrderBy(o => o.ts);

            var mock = new Mock<IMSBuildNodeTraceEventProcessor>();
            mock.Setup(o => o.YieldEvents(0)).Returns(input);
            var sut = new TraceEventCompressor(mock.Object);
            var actual = sut.YieldEvents(0).ToList();
            AssertEvents(s_testCaseData[key].Expected, actual);
        }

        private static void AssertEvents(TraceEvent[] expected, List<TraceEvent> actual)
        {
            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; ++i)
            {
                Assert.AreEqual(expected[i].name, actual[i].name);
                Assert.AreEqual(expected[i].ts, actual[i].ts);
                Assert.AreEqual(expected[i].dur, actual[i].dur);
                CollectionAssert.AreEqual(expected[i].args, actual[i].args);
            }
        }

        private IEnumerable<TraceEvent> StringToTraceEvents(string input)
        {
            char first = ' ';
            int start = -1;
            int i;
            for (i = 0; i < input.Length; ++i)
            {
                var c = input[i];
                if (first == c)
                {
                    continue;
                }

                if (first != ' ')
                {
                    yield return new()
                    {
                        name = first.ToString(),
                        ts = (uint)start,
                        dur = (uint)(i - start),
                    };
                }

                first = c;
                start = i;
            }
            if (first != ' ')
            {
                yield return new()
                {
                    name = first.ToString(),
                    ts = (uint)start,
                    dur = (uint)(i - start),
                };
            }
        }
    }
}