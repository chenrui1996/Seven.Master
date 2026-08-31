import { hitTestNode, NODE_HEIGHT, NODE_WIDTH } from './hitTest'

function assert(condition: boolean, message: string) {
  if (!condition) throw new Error(message)
}

const nodes = [
  { id: 'a', x: 0, y: 0 },
  { id: 'b', x: 100, y: 0 },
  { id: 'c', x: 0, y: 80 },
]

assert(hitTestNode(nodes, 10, 10) === 'a', 'point inside first node')
assert(hitTestNode(nodes, NODE_WIDTH - 1, NODE_HEIGHT - 1) === 'a', 'point at bottom-right of first node')
assert(hitTestNode(nodes, 110, 10) === 'b', 'point inside second node')
assert(hitTestNode(nodes, 10, 90) === 'c', 'point inside third node')
assert(hitTestNode(nodes, -1, 10) === null, 'point left of first node')
assert(hitTestNode(nodes, 50, NODE_HEIGHT + 5) === null, 'point between rows')
assert(hitTestNode([], 0, 0) === null, 'empty node list')

// Later nodes win when overlapping (top-most)
const stacked = [
  { id: 'bottom', x: 0, y: 0 },
  { id: 'top', x: 20, y: 10 },
]
assert(hitTestNode(stacked, 30, 20) === 'top', 'top-most overlapping node wins')

console.log(`hitTest: ${8} assertions passed`)
