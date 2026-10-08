export const prototypeImages = {
	avatar: '/static/logo.png',
	homeHero: '/static/prototype/home-hero.jpg',
	storeOne: '/static/prototype/home-hero.jpg',
	storeTwo: '/static/prototype/store-city.jpg',
	hostel: '/static/prototype/store-hostel.jpg',
	roomOne: '/static/prototype/home-hero.jpg',
	roomTwo: '/static/prototype/store-city.jpg',
	roomThree: '/static/prototype/home-hero.jpg',
	roomFour: '/static/prototype/store-city.jpg',
	article: '/static/prototype/store-hostel.jpg',
	articleDetail: '/static/prototype/home-hero.jpg'
}

export const demoStores = [
	{
		id: 1,
		featuredRoomId: 1,
		name: '云舍民宿·郑州二七路店',
		shortName: '云舍·二七店',
		status: '营业中',
		badge: '官方直营',
		rating: '4.9',
		reviews: '1280',
		distance: '10.88km',
		address: '河南省郑州市金水区郑路辅路28号',
		price: '300',
		unknownDistance: false,
		phone: '037188886622',
		latitude: 34.74725,
		longitude: 113.62493,
		tags: ['私家花园', '超赞房东', '近地铁1号线'],
		image: prototypeImages.storeOne,
		images: [prototypeImages.storeOne, prototypeImages.storeTwo, prototypeImages.hostel]
	},
	{
		id: 2,
		featuredRoomId: 2,
		name: '城市花园民宿·郑东如意湖店',
		shortName: '城市花园·如意湖店',
		status: '营业中',
		badge: '湖景露台',
		rating: '4.8',
		reviews: '890',
		distance: '2.50km',
		address: '郑东新区商务内环路世博大厦旁',
		price: '368',
		unknownDistance: false,
		phone: '037188886633',
		latitude: 34.75672,
		longitude: 113.68421,
		tags: ['落日景观', '提供精品早餐', '近CBD会展中心'],
		image: prototypeImages.storeTwo,
		images: [prototypeImages.storeTwo, prototypeImages.storeOne]
	},
	{
		id: 3,
		featuredRoomId: 3,
		name: '青年旅舍床位专区·郑州大学科技园店',
		shortName: '青年旅舍·科技园店',
		status: '营业中',
		badge: '青年创客',
		rating: '4.9',
		reviews: '452',
		distance: '4.80km',
		address: '高新区科学大道与长椿路交汇处南',
		price: '69',
		unknownDistance: false,
		phone: '037188886644',
		latitude: 34.81944,
		longitude: 113.53214,
		tags: ['四人独立卫浴', '自习阅读区', '青年社交沙龙'],
		image: prototypeImages.hostel,
		images: [prototypeImages.hostel]
	},
	{
		id: 4,
		featuredRoomId: 1,
		name: '云舍生态度假村·伏羲山隐庐店',
		shortName: '云舍生态度假村',
		status: '营业中',
		badge: '山野私享',
		rating: '4.95',
		reviews: '206',
		distance: '距离未知',
		address: '新密市伏羲山风景区观景台西侧',
		price: '520',
		unknownDistance: true,
		phone: '037188886655',
		latitude: 34.78231,
		longitude: 113.40962,
		tags: ['山景套房', '免费停车', '围炉煮茶'],
		image: prototypeImages.roomFour
	}
]

export const demoRooms = [
	{
		id: 1,
		name: '阳光实木标准大床房',
		type: '独立房间',
		gender: '不限性别',
		capacity: '可住2人',
		beds: '1张1.8m大床',
		area: '28㎡',
		price: '300',
		originalPrice: '360',
		availability: '今日可预订 · 仅剩2间',
		tags: ['独立卫浴', '千兆Wi-Fi', '采光飘窗', '免费早餐'],
		image: prototypeImages.roomOne
	},
	{
		id: 2,
		name: '城市花园观景双床房',
		type: '独立房间',
		gender: '不限性别',
		capacity: '可住4人',
		beds: '2张1.5m双人床',
		area: '35㎡',
		price: '368',
		originalPrice: '',
		availability: '今日可预订 · 库存充足',
		tags: ['私密露台', '独立卫浴', '全景落地窗', '地暖空调'],
		image: prototypeImages.roomTwo
	},
	{
		id: 3,
		name: '青年创客四人间单床位（女宾专区）',
		type: '多人间床位',
		gender: '女生专区',
		capacity: '单人床位',
		beds: '1张1.2m实木床',
		area: '30㎡共享空间',
		price: '69',
		originalPrice: '',
		availability: '今日可预订 · 余3床位',
		tags: ['遮光私密床帘', '独立密码柜', '床头静音插座', '套内独立卫浴'],
		image: prototypeImages.hostel
	},
	{
		id: 4,
		name: '日式庭院榻榻米套房',
		type: '整套房',
		gender: '不限性别',
		capacity: '可住3人',
		beds: '榻榻米日式垫',
		area: '38㎡',
		price: '420',
		originalPrice: '',
		availability: '暂时无房 · 最近可订10/03',
		tags: ['日式枯山水庭院', '手工茶具', '智能客控'],
		image: prototypeImages.roomThree,
		soldOut: true
	}
]

export const demoOrders = [
	{
		id: 'TCP202610010001',
		store: '云舍民宿·郑州二七路店',
		room: '阳光实木标准大床房',
		status: '已确认·待入住',
		statusKey: 'stay',
		date: '10月03日 - 10月05日',
		guest: '张三 等2人',
		price: '536',
		image: prototypeImages.roomOne,
		action: '申请退款'
	},
	{
		id: 'TCP202610010002',
		store: '青年旅舍床位专区·郑州大学科技园店',
		room: '女生四人间单床位',
		status: '待支付',
		statusKey: 'pay',
		date: '10月06日 - 10月08日',
		guest: '李四',
		price: '176',
		image: prototypeImages.hostel,
		action: '立即支付'
	},
	{
		id: 'TCP202609280008',
		store: '城市花园民宿·郑东如意湖店',
		room: '城市花园观景双床房',
		status: '退款审核中',
		statusKey: 'refund',
		date: '10月09日 - 10月10日',
		guest: '王芳',
		price: '368',
		image: prototypeImages.storeTwo,
		action: '查看退款进度'
	},
	{
		id: 'TCP202609150003',
		store: '云舍民宿·郑州二七路店',
		room: '阳光实木标准大床房',
		status: '已完成',
		statusKey: 'done',
		date: '09月15日 - 09月16日',
		guest: '张三',
		price: '300',
		image: prototypeImages.roomOne,
		action: '再次预订'
	}
]

export const demoGuests = [
	{
		id: 1,
		name: '张三',
		identity: '410***********1234',
		phone: '138****8888',
		tag: '默认入住人',
		isDefault: true
	},
	{
		id: 2,
		name: '李四',
		identity: '410***********5678',
		phone: '139****9999',
		tag: '同行人',
		isDefault: false
	}
]

export const showToast = (title) => {
	uni.showToast({
		title,
		icon: 'none',
		duration: 1600
	})
}

export const showPrototypeToast = showToast

export const goPage = (path) => {
	uni.navigateTo({ url: path })
}

export const goRoot = (path) => {
	uni.reLaunch({ url: path })
}
