#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using NUnit.Framework;
using XerahS.Core;
using XerahS.UI.Services;

namespace XerahS.Tests.Helpers;

[TestFixture]
public class UpdateServiceRepositoryTests
{
    [TestCase(UpdateChannel.Release)]
    [TestCase(UpdateChannel.PreRelease)]
    public void ResolveUpdateRepositories_EveryChannel_UsesOnlyTheFork(UpdateChannel channel)
    {
        var settings = new ApplicationConfig { UpdateChannel = channel };

        IReadOnlyList<(string Owner, string Repo)> repositories = UpdateService.ResolveUpdateRepositories(settings);

        Assert.That(repositories, Is.EqualTo(new[] { ("yuxi-liu-wired", "XerahS") }));
        Assert.That(UpdateService.ResolveUpdateRepository(settings), Is.EqualTo(("yuxi-liu-wired", "XerahS")));
    }
}
