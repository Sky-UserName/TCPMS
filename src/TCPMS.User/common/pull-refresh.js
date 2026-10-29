const MIN_INDICATOR_MS = 320

export const runPullRefresh = async (page, loadData) => {
	if (page.pullRefreshing) return

	page.pullRefreshing = true
	const startedAt = Date.now()
	try {
		await loadData()
	} catch (error) {
		uni.showToast({ title: error?.message || '刷新失败，已保留当前内容', icon: 'none' })
	} finally {
		const remaining = MIN_INDICATOR_MS - (Date.now() - startedAt)
		if (remaining > 0) await new Promise((resolve) => setTimeout(resolve, remaining))
		page.pullRefreshing = false
		uni.stopPullDownRefresh()
	}
}
