/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Communication.Contracts.Hosts;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Monitoring.Contracts;
using SafeExamBrowser.Runtime.Communication;
using SafeExamBrowser.Runtime.Operations.Session;
using SafeExamBrowser.Settings;
using SafeExamBrowser.UserInterface.Contracts.MessageBox;
using SafeExamBrowser.UserInterface.Contracts.Windows;

namespace SafeExamBrowser.Runtime.UnitTests.Operations.Session
{
	[TestClass]
	public class RemoteSessionOperationTests
	{

		private RemoteSessionOperation sut;

		[TestInitialize]
		public void Initialize()
		{
	
		}

		[TestMethod]
		public void Perform_MustAbortIfRemoteSessionNotAllowed()
		{

		}

		[TestMethod]
		public void Perform_MustSucceedIfRemoteSessionAllowed()
		{

		}

		[TestMethod]
		public void Perform_MustSucceedIfNoRemoteSession()
		{

		}

		[TestMethod]
		public void Repeat_MustAbortIfRemoteSessionNotAllowed()
		{

		}

		[TestMethod]
		public void Repeat_MustSucceedIfRemoteSessionAllowed()
		{

		}

		[TestMethod]
		public void Repeat_MustSucceedIfNoRemoteSession()
		{

		}

		[TestMethod]
		public void Revert_MustDoNoting()
		{

		}
	}
}
