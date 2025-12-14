# 🚀 NuGet OIDC 快速配置指南

## 📋 配置清单（3 步完成）

### ✅ 第 1 步：在 NuGet.org 配置 Trusted Publisher

#### 如果包还不存在：
1. 先手动发布一次初始版本（使用传统方式）
2. 或访问 https://www.nuget.org/packages/manage/upload 预留包名

#### 配置 Trusted Publisher：
```
访问: https://www.nuget.org/packages/Arc.UniInk/manage

找到 "Trusted publishers" 部分
点击 "Add trusted publisher"

填写信息：
┌─────────────────────────────────────────────┐
│ Publisher type: GitHub Actions              │
│ Owner: Arc-huangjingtong                    │
│ Repository: UniInk-CSharpInterpreter4AOT    │
│ Workflow: build_nuget.yml                   │
│ Environment: (留空)                          │
└─────────────────────────────────────────────┘

点击 "Add" 保存
```

---

### ✅ 第 2 步：在 GitHub 添加 Secret

```
仓库 Settings → Secrets and variables → Actions → New secret

┌─────────────────────────────────────────────┐
│ Name: NUGET_USER                            │
│ Value: [您的 NuGet.org 用户名]               │
└─────────────────────────────────────────────┘

点击 "Add secret"
```

💡 **查找用户名**: https://www.nuget.org/account

---

### ✅ 第 3 步：推送工作流并测试

```bash
# 推送工作流文件
git add .github/workflows/build_nuget.yml
git commit -m "配置 NuGet Trusted Publishing (OIDC)"
git push origin main

# 测试发布
git tag v0.1.0
git push origin v0.1.0
```

---

## 🎯 就是这么简单！

配置完成后，每次发布只需：

```bash
git tag v0.2.0
git push origin v0.2.0
```

GitHub Actions 会自动：
- ✅ 使用 OIDC 获取临时令牌
- ✅ 构建并发布 NuGet 包
- ✅ 创建 GitHub Release

---

## ❓ 遇到问题？

### 🔴 OIDC 认证失败
```
检查清单：
□ NuGet.org 上是否配置了 Trusted Publisher？
□ 仓库名、工作流名是否完全匹配？
□ NUGET_USER Secret 是否正确？
□ 工作流是否有 id-token: write 权限？
```

### 🔴 首次发布失败
```
首次需要先有包才能配置 Trusted Publisher

方案 1：手动发布首个版本
  dotnet pack ... -c Release
  上传到 https://www.nuget.org/packages/manage/upload

方案 2：使用传统 API Key 发布首个版本
  然后再配置 OIDC
```

---

## 📚 详细文档

完整配置说明请查看：
- `NUGET_TRUSTED_PUBLISHING_GUIDE.md`

---

**配置完成后享受自动化发布！** 🎉