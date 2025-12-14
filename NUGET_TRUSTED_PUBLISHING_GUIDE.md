# 🔐 NuGet Trusted Publishing (OIDC) 配置指南

本项目使用 **NuGet Trusted Publishing**（基于 OIDC）进行自动化发布，无需手动管理长期 API Key，更加安全便捷。

---

## 📋 什么是 Trusted Publishing？

Trusted Publishing 使用 **OpenID Connect (OIDC)** 技术，让 GitHub Actions 可以直接获取短期 API Key 来发布包，无需手动创建和存储长期 API Key。

### ✨ 优势

- ✅ **更安全**: 不需要存储长期 API Key
- ✅ **自动化**: GitHub Actions 自动获取临时令牌
- ✅ **简单**: 配置一次，永久有效
- ✅ **可追溯**: 所有发布都有完整的审计日志

---

## ⚙️ 配置步骤

### 第 1 步：在 NuGet.org 上配置 Trusted Publishing

#### 1.1 登录 NuGet.org

访问 https://www.nuget.org/ 并登录您的账号

#### 1.2 创建或选择包

如果是首次发布，您需要先预留包名：

1. 访问 https://www.nuget.org/packages/manage/upload
2. 上传一个临时的 `.nupkg` 文件来预留包名
3. 或者等待第一次手动发布后再配置 Trusted Publishing

#### 1.3 配置 Trusted Publisher

1. 访问您的包管理页面：https://www.nuget.org/packages/Arc.UniInk/manage
2. 找到 **"Trusted publishers"** 部分
3. 点击 **"Add trusted publisher"**
4. 填写以下信息：

   ```
   Publisher type: GitHub Actions
   Owner: Arc-huangjingtong
   Repository: UniInk-CSharpInterpreter4AOT
   Workflow: build_nuget.yml
   Environment (可选): 留空或填写 "production"
   ```

5. 点击 **"Add"** 保存配置

> **注意**: 如果包还不存在，您需要先手动发布第一个版本，然后再配置 Trusted Publishing。

---

### 第 2 步：在 GitHub 仓库中添加 Secret

虽然使用 OIDC 不需要 API Key，但需要配置您的 NuGet 用户名：

1. 打开 GitHub 仓库
2. 进入 **Settings** > **Secrets and variables** > **Actions**
3. 点击 **"New repository secret"**
4. 添加以下 Secret：
   - **Name**: `NUGET_USER`
   - **Value**: 您的 NuGet.org **用户名**（profile name，不是邮箱）
5. 点击 **"Add secret"**

> 💡 **如何找到用户名**：访问 https://www.nuget.org/account 查看您的 "Username"

---

### 第 3 步：推送工作流文件到 GitHub

```bash
cd /Users/Zhuanz/Documents/GitHub/UniInk-CSharpInterpreter4AOT

git add .github/workflows/build_nuget.yml
git commit -m "配置 NuGet Trusted Publishing (OIDC)"
git push origin main
```

---

## 🚀 使用方法

配置完成后，发布流程与之前相同：

### 方法一：通过 Git 标签自动触发（推荐）

```bash
# 创建版本标签
git tag v0.1.0

# 推送标签
git push origin v0.1.0

# GitHub Actions 会自动：
# 1. 使用 OIDC 获取临时 API Key
# 2. 构建并发布 NuGet 包
# 3. 创建 GitHub Release
```

### 方法二：手动触发

```
1. GitHub 仓库 → Actions 标签
2. 选择 "Build and Publish NuGet (OIDC)"
3. 点击 "Run workflow"
4. 输入版本号（可选）
5. 点击 "Run workflow"
```

---

## 🔍 工作流程详解

### OIDC 认证流程

```
┌─────────────────┐
│ GitHub Actions  │
│  触发工作流     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 请求 OIDC Token │ ← GitHub 颁发身份令牌
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  NuGet.org      │
│ 验证 OIDC Token │ ← 验证仓库、工作流等信息
│ 颁发临时 API Key│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 使用 API Key    │
│  发布 NuGet 包  │
└─────────────────┘
```

### 关键步骤

1. **OIDC 认证**
   ```yaml
   - name: 🔐 NuGet 登录 (OIDC → 临时 API Key)
     uses: NuGet/login@v1
     with:
       user: ${{ secrets.NUGET_USER }}
   ```

2. **获取临时 API Key**
   - GitHub Actions 自动颁发 OIDC Token
   - NuGet.org 验证并返回临时 API Key
   - API Key 仅在当前工作流中有效

3. **发布包**
   ```yaml
   - name: 🚀 发布到 NuGet.org
     run: dotnet nuget push ./artifacts/*.nupkg \
       --api-key ${{ steps.nuget_login.outputs.NUGET_API_KEY }}
   ```

---

## 🆚 OIDC vs 传统 API Key 对比

| 特性 | OIDC Trusted Publishing | 传统 API Key |
|------|------------------------|-------------|
| **安全性** | ✅ 极高（临时令牌） | ⚠️ 中等（长期密钥） |
| **配置复杂度** | 🟡 中等（需配置 Publisher） | 🟢 简单（创建 Key） |
| **维护成本** | ✅ 低（无需轮换） | ⚠️ 中等（需定期轮换） |
| **泄露风险** | ✅ 极低（令牌短期有效） | ⚠️ 高（如泄露需立即撤销） |
| **审计能力** | ✅ 完整（关联到工作流） | 🟡 有限（仅知道 API Key） |
| **首次发布** | ⚠️ 需要先配置 | ✅ 直接使用 |

---

## ❓ 常见问题

### ❓ 首次发布如何配置？

**两种方式**：

**方式 1：先手动发布一次**
```bash
# 生成包
dotnet pack Arc.UniInk/Arc.UniInk/Arc.UniInk.csproj -c Release

# 使用网页上传或临时 API Key 发布第一个版本
# 然后在 NuGet.org 上配置 Trusted Publisher
```

**方式 2：先预留包名**
```
1. 访问 https://www.nuget.org/packages/manage/upload
2. 上传一个初始版本（如 0.0.1）
3. 在包管理页面配置 Trusted Publisher
4. 使用 GitHub Actions 发布正式版本
```

### ❓ OIDC 认证失败

**错误**: `Failed to get OIDC token` 或 `401 Unauthorized`

**解决方案**：
1. ✅ 检查工作流是否有 `id-token: write` 权限
2. ✅ 确认在 NuGet.org 上正确配置了 Trusted Publisher
3. ✅ 检查仓库名、工作流文件名是否完全匹配
4. ✅ 确认 `NUGET_USER` Secret 设置正确

### ❓ 如何查看 Trusted Publisher 配置？

```
1. 登录 NuGet.org
2. 访问包管理页面：
   https://www.nuget.org/packages/Arc.UniInk/manage
3. 滚动到 "Trusted publishers" 部分查看
```

### ❓ 可以同时使用 OIDC 和传统 API Key 吗？

**可以**！配置 Trusted Publishing 后，传统 API Key 仍然有效：
- GitHub Actions 使用 OIDC（推荐）
- 本地开发可以使用传统 API Key
- 两者互不影响

### ❓ 如何撤销 Trusted Publisher？

```
1. 访问包管理页面
2. 找到 "Trusted publishers" 部分
3. 点击对应配置旁的 "Remove" 按钮
```

### ❓ 可以为多个仓库配置 Trusted Publisher 吗？

**可以**！同一个包可以配置多个 Trusted Publisher，例如：
- 主仓库的工作流
- Fork 仓库的工作流（如果您允许）
- 不同的工作流文件

---

## 🔒 安全最佳实践

### ✅ 推荐做法

1. **限制工作流触发条件**
   ```yaml
   on:
     push:
       tags:
         - 'v*.*.*'  # 仅标签触发
   ```

2. **使用环境保护**
   ```yaml
   jobs:
     build-and-publish:
       environment: production  # 需要审批才能发布
   ```

3. **启用必需权限**
   ```yaml
   permissions:
     id-token: write    # OIDC 必需
     contents: write    # 创建 Release
   ```

4. **审查工作流更改**
   - Pull Request 中的工作流更改需要仔细审查
   - 考虑使用 CODEOWNERS 保护工作流文件

### ❌ 避免

- ❌ 不要在公开的工作流中硬编码用户名
- ❌ 不要将 Trusted Publisher 配置给不信任的仓库
- ❌ 不要禁用 `id-token: write` 权限

---

## 📊 监控和审计

### 查看发布历史

**GitHub Actions**
```
仓库 → Actions → 查看工作流运行历史
可以看到每次发布的完整日志
```

**NuGet.org**
```
包管理页面 → Statistics
查看下载量、版本历史等
```

### 审计日志

NuGet Trusted Publishing 提供完整的审计跟踪：
- 发布时间
- 触发的工作流
- GitHub 仓库和提交信息
- OIDC Token 信息

---

## 🔄 从传统 API Key 迁移

如果您之前使用 `publish-nuget.yml` 工作流（传统 API Key 方式）：

### 迁移步骤

1. **保留旧工作流**（可选）
   ```bash
   # 重命名旧工作流作为备份
   mv .github/workflows/publish-nuget.yml .github/workflows/publish-nuget.yml.backup
   ```

2. **配置 Trusted Publisher**
   - 在 NuGet.org 上配置（见上文）

3. **添加 NUGET_USER Secret**
   - 在 GitHub 仓库中添加（见上文）

4. **使用新工作流**
   - `build_nuget.yml` 已经配置好了

5. **测试发布**
   ```bash
   git tag v0.1.1-test
   git push origin v0.1.1-test
   ```

6. **删除旧的 API Key**（可选）
   - 在 NuGet.org 上撤销旧的 API Key
   - 在 GitHub Secrets 中删除 `NUGET_API_KEY`

---

## 📚 参考资料

- 📖 [NuGet Trusted Publishing 官方文档](https://learn.microsoft.com/zh-cn/nuget/nuget-org/trusted-publishing)
- 🔐 [GitHub Actions OIDC 文档](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- 🔗 [NuGet/login Action](https://github.com/NuGet/login)
- 📦 [您的包管理页面](https://www.nuget.org/packages/Arc.UniInk/manage)

---

## 🎯 快速配置检查清单

使用此清单确保配置完整：

- [ ] 在 NuGet.org 上配置了 Trusted Publisher
  - [ ] Owner: `Arc-huangjingtong`
  - [ ] Repository: `UniInk-CSharpInterpreter4AOT`
  - [ ] Workflow: `build_nuget.yml`
- [ ] 在 GitHub 中添加了 `NUGET_USER` Secret
- [ ] 工作流文件已推送到 GitHub
- [ ] 工作流有 `id-token: write` 权限
- [ ] 测试标签发布成功

---

## 🎉 总结

使用 NuGet Trusted Publishing (OIDC)，您可以：

✅ **无需管理 API Key** - 完全自动化  
✅ **更高的安全性** - 短期令牌，降低泄露风险  
✅ **完整的审计跟踪** - 每次发布都关联到 GitHub 工作流  
✅ **简化维护** - 配置一次，永久有效  

**配置完成后，发布就像推送标签一样简单：**

```bash
git tag v0.1.0
git push origin v0.1.0
```

就是这么简单！🚀