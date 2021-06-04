//
// Copyright 2021 CEXIOLABS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

namespace CEXIOLABS.CommunitySoft.Notifier.Lib.Service
{
	using System;
	using System.Threading.Tasks;

	using Model;

	public interface ISecurityTokenService
	{
		Task<string> EncodeTopicManageToken(Topic.Id topicId, ManageTokenPermission permissionFlags);
		Task<string> EncodeTopicOwnerToken(Topic.Id topicId);
		Task<Topic.Id> DecodeTopicManageToken(string topicManageToken);
		Task<Topic.Id> DecodeTopicOwnerToken(string topicOwnerToken);
	}

	public class FakeSecurityTokenService : ISecurityTokenService
	{
		public interface Options
		{
			// Add necessary stuff
		}

		public FakeSecurityTokenService(Options options)
		{
			// Add necessary stuff
		}

		public Task<string> EncodeTopicManageToken(Topic.Id topicId, ManageTokenPermission permissionFlags)
		{
			throw new NotImplementedException();
		}

		public Task<string> EncodeTopicOwnerToken(Topic.Id topicId)
		{
			throw new NotImplementedException();
		}

		public Task<Topic.Id> DecodeTopicManageToken(string topicManageToken)
		{
			throw new NotImplementedException();
		}

		public Task<Topic.Id> DecodeTopicOwnerToken(string topicOwnerToken)
		{
			throw new NotImplementedException();
		}

		// public async Task<string> CreateSecurityToken(Topic topic)
		// {
		// 	string fakeToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(topic.QualifiedName));
		// 	return fakeToken;
		// }

		// public async Task<string> ParseTopicSecurityToken(string topicFakeSecurityToken)
		// {
		// 	string qualifiedName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(topicFakeSecurityToken));
		// 	return qualifiedName;
		// }
	}

}